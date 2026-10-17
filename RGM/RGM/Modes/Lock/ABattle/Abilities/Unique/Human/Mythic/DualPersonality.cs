using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features.Items;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.ToggleableLights;
using MEC;
using RGM.API.DataBases;
using RGM.API.Features;
using RGM.Features;
using RGM.Variables;
using UnityEngine;

namespace RGM.Modes.Abilities.Unique.Human.Mythic;

[Ability("이중인격",
    """
    두 가지 형상, <color=#FFE6EA>연극</color>과 <color=#191970>환멸</color>의 모습으로 변신합니다.
    지급된 아이템을 사용하여 인격을 변경할 수 있으며, 각 인격은 특정 종류의 능력치를 크게 강화시킵니다.
    """, 
    AbilityCategory.Mythic, AbilityType.MYTHIC_HUMAN_DUALPERSONALITY, RoleAbility.Human, isUnique:true)]

public class DualPersonality : Ability
{
    private enum Form
    {
        /// <summary>연극의 형상 (Lantern ON)</summary>
        Drama,

        /// <summary>환멸의 형상 (Lantern OFF)</summary>
        Disillusion,
    }

    // 형상 전환
    private const float TransitionDuration = 0.75f;

    // 연극의 형상
    private const float DramaHealInterval = 0.5f;
    private const float DramaHealRatio = 0.08f;
    private const float DramaAhpSustain = 30f;
    private const float DramaMaxAhpRatio = 0.8f;
    private const int DramaSpeedReduction = 40;
    private const float DramaDamageMultiplier = 0.6f;
    private const float DramaLifeStealRatio = 0.8f;
    private const float DramaMaxIncomeDamage = 20f;

    // 환멸의 형상
    private const float DisillusionTickInterval = 1f;
    private const float DisillusionSelfDamageRatio = 0.05f;
    private const float DisillusionMinHealthRatio = 0.2f;
    private const int DisillusionBoostIntensity = 80;
    private const float DisillusionDamageMultiplier = 1.8f;

    private Form _form;
    private ushort _lanternSerial;
    private float _lockedUntil;

    private bool _isSpeedOverridden;
    private bool _isApplyingFormEffect;
    private int _originalBoost;
    private int _originalSlowness;

    private CoroutineHandle _formRoutine;
    private CoroutineHandle _syncRoutine;

    public override void OnEnabled()
    {
        GiveLantern();

        Exiled.Events.Handlers.Player.TogglingFlashlight += OnTogglingFlashlight;
        Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.Hurting += OnHurting;
        Exiled.Events.Handlers.Player.Hurt += OnHurt;
        Exiled.Events.Handlers.Player.ReceivingEffect += OnReceivingEffect;

        // 능력 획득 시 기본 형상은 연극 (Lantern ON)
        _form = Form.Drama;
        _lockedUntil = 0f;
        SetLanternState(true);
        EnterForm(Form.Drama);

        _syncRoutine = Timing.RunCoroutine(SyncLanternRoutine());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.TogglingFlashlight -= OnTogglingFlashlight;
        Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.Hurting -= OnHurting;
        Exiled.Events.Handlers.Player.Hurt -= OnHurt;
        Exiled.Events.Handlers.Player.ReceivingEffect -= OnReceivingEffect;

        Timing.KillCoroutines(_syncRoutine);
        ExitForm(_form);

        Item lantern = GetLantern();
        if (lantern != null)
            Owner.RemoveItem(lantern);
    }

    #region 형상 전환

    private void OnTogglingFlashlight(TogglingFlashlightEventArgs ev)
    {
        if (ev.Player != Owner || ev.Item?.Serial != _lanternSerial)
            return;

        // 혼동 방지를 위해 전환 직후에는 아이템 상호작용을 막는다.
        if (Time.time < _lockedUntil)
        {
            ev.IsAllowed = false;
            return;
        }

        SetForm(ev.NewState ? Form.Drama : Form.Disillusion);
    }

    /// <summary>
    /// 이벤트가 발생하지 않는 경우를 대비해 Lantern 상태와 형상을 동기화합니다.
    /// </summary>
    private IEnumerator<float> SyncLanternRoutine()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(0.1f);

            if (Owner == null || !Owner.IsAlive)
                continue;

            // SeveredHands의 강제 드롭이나 914 기반 아이템 교체는 DroppingItem 이벤트를
            // 우회할 수 있으므로, 랜턴의 소유 및 종류를 계속 검증하여 복구한다.
            if (!EnsureLantern())
                continue;

            ToggleableLightItemBase light = (ToggleableLightItemBase)GetLantern().Base;
            Form lanternForm = light.IsEmittingLight ? Form.Drama : Form.Disillusion;
            if (lanternForm == _form)
                continue;

            if (Time.time < _lockedUntil)
                SetLanternState(_form == Form.Drama);
            else
                SetForm(lanternForm);
        }
    }

    private void SetForm(Form next)
    {
        if (_form == next)
            return;

        // MEC의 WaitForOneFrame은 yield 전용 표식이므로 시간 계산에 사용하지 않는다.
        // 전환 효과를 적용하기 전에 잠금을 시작해 처리 중 발생하는 추가 토글도 막는다.
        _lockedUntil = Time.time + TransitionDuration * 2;

        ExitForm(_form);
        _form = next;
        EnterForm(next);

        ApplyTransitionEffects();

        string formName = next == Form.Drama ? "<color=#FFE6EA>연극</color>" : "<color=#191970>환멸</color>";
        Owner.AddHint("이중인격", $"{formName}의 형상으로 변신했습니다.");
    }

    private void EnterForm(Form form)
    {
        switch (form)
        {
            case Form.Drama:
                ApplyDramaSpeed();
                _formRoutine = Timing.RunCoroutine(DramaRoutine());
                break;

            case Form.Disillusion:
                Owner.AddEffect(EffectType.MovementBoost, DisillusionBoostIntensity);
                _formRoutine = Timing.RunCoroutine(DisillusionRoutine());
                break;
        }
    }

    private void ExitForm(Form form)
    {
        Timing.KillCoroutines(_formRoutine);

        switch (form)
        {
            case Form.Drama:
                RestoreSpeed();
                break;

            case Form.Disillusion:
                Owner.RemoveEffect(EffectType.MovementBoost, DisillusionBoostIntensity);
                break;
        }
    }

    /// <summary>
    /// 전환 시 1.5초 생존 효과(무적 + 은신)를 부여하고, 동시에 0.75초간 이동 불가 상태를 만든다.
    /// 이동속도 보정과 충돌하지 않도록 『생존』의 이동속도 증가는 적용하지 않는다.
    /// </summary>
    private void ApplyTransitionEffects()
    {
        ApplyFormEffect(() =>
        {
            Owner.AddEffect(EffectType.Invisible, 1, TransitionDuration * 2);
            Owner.AddEffect(EffectType.Ghostly, 1, TransitionDuration * 2);
            Owner.ApplyGodMode(TransitionDuration);

            Owner.AddEffect(EffectType.SinkHole, 1, TransitionDuration);
            Owner.AddEffect(EffectType.Ensnared, 1, TransitionDuration);
        });
    }

    #endregion

    #region 연극의 형상

    private IEnumerator<float> DramaRoutine()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(DramaHealInterval);

            if (Owner == null || !Owner.IsAlive)
                yield break;

            float amount = Mathf.Min(Owner.MaxHealth * DramaHealRatio, Owner.MaxHealth - Owner.Health);
            if (amount <= 0f)
                continue;

            Owner.Heal(amount);

            // 실제 HP 회복량만큼 AHP와 최대 AHP를 함께 증가시키되, 최대 HP의 80%를 넘지 않게 한다.
            float limit = Mathf.Min(Owner.MaxArtificialHealth + amount, Owner.MaxHealth * DramaMaxAhpRatio);
            Owner.MaxArtificialHealth = limit;
            Owner.AddAhp(amount, limit, sustain: DramaAhpSustain);
        }
    }

    /// <summary>
    /// 현재 이동속도 증감량(MovementBoost - Slowness)을 계산한 뒤 -40을 적용하여,
    /// 결과가 양수면 MovementBoost, 음수면 Slowness를 적용한다. (0이면 아무것도 적용하지 않음)
    /// </summary>
    private void ApplyDramaSpeed()
    {
        _originalBoost = GetIntensity(EffectType.MovementBoost);
        _originalSlowness = GetIntensity(EffectType.Slowness);
        _isSpeedOverridden = true;

        int speed = _originalBoost - _originalSlowness - DramaSpeedReduction;

        ApplyFormEffect(() =>
        {
            SetEffectIntensity(EffectType.MovementBoost, Math.Max(speed, 0));
            SetEffectIntensity(EffectType.Slowness, Math.Max(-speed, 0));
        });
    }

    private void RestoreSpeed()
    {
        if (!_isSpeedOverridden)
            return;

        _isSpeedOverridden = false;

        if (Owner == null || !Owner.IsAlive)
            return;

        ApplyFormEffect(() =>
        {
            SetEffectIntensity(EffectType.MovementBoost, GetBaseIntensity(EffectType.MovementBoost, _originalBoost));
            SetEffectIntensity(EffectType.Slowness, GetBaseIntensity(EffectType.Slowness, _originalSlowness));
        });
    }

    #endregion

    #region 환멸의 형상

    private IEnumerator<float> DisillusionRoutine()
    {
        while (true)
        {
            yield return Timing.WaitForSeconds(DisillusionTickInterval);

            if (Owner == null || !Owner.IsAlive)
                yield break;

            // 최대 체력의 20% 아래로는 감소하지 않는다.
            float minHealth = Owner.MaxHealth * DisillusionMinHealthRatio;
            float damage = Math.Min(Owner.MaxHealth * DisillusionSelfDamageRatio, Owner.Health - minHealth);

            if (damage > 0f)
                Owner.Health -= damage;
        }
    }

    #endregion

    #region 공격

    private void OnHurting(HurtingEventArgs ev)
    {
        if (_form == Form.Drama && ev.Player == Owner)
        {
            // 즉사 피해(SCP-173 등)는 Damage가 -1(KillValue)이라 Math.Min으로는 상한이 적용되지 않고,
            // 게임이 방어/체력 계산 없이 바로 사망 처리한다. 상한 피해로 바꿔 일반 피해 계산을 거치게 한다.
            // 시스템 사망(BlockDamageTypes)은 기존처럼 막지 않는다.
            if (ev.IsInstantKill)
            {
                if (!Datas.BlockDamageTypes.Contains(ev.DamageHandler.Type))
                    ev.DamageHandler.Damage = DramaMaxIncomeDamage;
            }
            else
            {
                ev.DamageHandler.Damage = Math.Min(ev.DamageHandler.Damage, DramaMaxIncomeDamage);
            }
        }

        if (!IsOwnerAttack(ev.Attacker, ev.Player, ev.DamageHandler.Type))
            return;

        ev.DamageHandler.Damage *= _form == Form.Drama ? DramaDamageMultiplier : DisillusionDamageMultiplier;
    }

    private void OnReceivingEffect(ReceivingEffectEventArgs ev)
    {
        if (_form != Form.Drama || _isApplyingFormEffect || ev.Player != Owner)
            return;

        EffectType effectType = ev.Effect.GetEffectType();
        if (ev.Intensity > 0 && !EffectManager.IsKeptBuff(effectType))
            ev.IsAllowed = false;
    }

    private void OnHurt(HurtEventArgs ev)
    {
        if (!IsOwnerAttack(ev.Attacker, ev.Player, ev.DamageHandler.Type))
            return;

        float damage = ev.DamageHandler.Damage;
        if (damage <= 0f)
            return;

        switch (_form)
        {
            case Form.Drama:
                Owner.Heal(damage * DramaLifeStealRatio);
                break;

            case Form.Disillusion:
                // 잃은 HP만큼 관통 고정 피해를 추가로 입힌다 (최대 3000까지 적용).
                float lostHealth = Owner.MaxHealth - Owner.Health;
                if (lostHealth > 0f && ApplyFixedDamage.Apply(Owner, ev.Player, Mathf.Min(lostHealth, 3000f)))
                    Owner.ShowHitMarker(1.25f);
                break;
        }
    }

    private bool IsOwnerAttack(Exiled.API.Features.Player attacker, Exiled.API.Features.Player target, DamageType damageType)
    {
        return attacker != null &&
               attacker == Owner &&
               target != null &&
               target != Owner &&
               !ApplyFixedDamage.IsApplying &&
               !Datas.BlockDamageTypes.Contains(damageType) &&
               HitboxIdentity.IsEnemy(attacker.ReferenceHub, target.ReferenceHub);
    }

    #endregion

    #region Lantern

    /// <summary>
    /// 형상 전환 수단인 랜턴이 강제 드롭되거나 다른 아이템으로 교체되었으면 새로 지급한다.
    /// </summary>
    private bool EnsureLantern()
    {
        Item lantern = GetLantern();
        if (lantern?.Base is ToggleableLightItemBase)
            return true;

        // 동일 Serial의 아이템이 다른 종류로 바뀐 경우, 해당 아이템을 제거한 뒤 랜턴으로 복구한다.
        if (lantern != null)
            Owner.RemoveItem(lantern);

        GiveLantern();
        return GetLantern()?.Base is ToggleableLightItemBase;
    }

    private void GiveLantern()
    {
        Item lantern = Owner.AddItem(ItemType.Lantern);
        _lanternSerial = lantern.Serial;
        SetLanternState(_form == Form.Drama);
    }

    private void OnDroppingItem(DroppingItemEventArgs ev)
    {
        if (ev.Player != Owner || ev.Item?.Serial != _lanternSerial)
            return;

        ev.IsAllowed = false;
        ev.Player.AddHint("이중인격", "형상을 변경하는 랜턴은 버릴 수 없습니다.", 1.5f);
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        if (ev.Player != Owner || ev.Item?.Serial != _lanternSerial)
            return;

        ev.Player.AddHint("이중인격",
            $"<b><color={ABattle.RatingColor["신화"]}>이중인격</color></b>의 랜턴입니다. 켜면 <b><color=#FFE6EA>연극</color></b>, 끄면 <b><color=#191970>환멸</color></b>의 형상이 됩니다.");
    }

    private Item GetLantern() => Owner?.Items.FirstOrDefault(x => x.Serial == _lanternSerial);

    private void SetLanternState(bool isOn)
    {
        if (GetLantern()?.Base is ToggleableLightItemBase light && light.IsEmittingLight != isOn)
            light.IsEmittingLight = isOn;
    }

    #endregion

    #region 효과 유틸

    private int GetIntensity(EffectType type)
    {
        StatusEffectBase effect = Owner.GetEffect(type);
        return effect is { IsEnabled: true } ? effect.Intensity : 0;
    }

    /// <summary>
    /// AddEffect로 누적 관리되는 값이 있으면 그 값을, 없으면 덮어쓰기 전에 저장한 값을 반환합니다.
    /// </summary>
    private int GetBaseIntensity(EffectType type, int fallback)
    {
        if (Variable.EffectIntensities.TryGetValue(Owner, out var intensities) &&
            intensities.TryGetValue(type, out int tracked))
            return Math.Min(tracked, byte.MaxValue);

        return fallback;
    }

    private void SetEffectIntensity(EffectType type, int intensity)
    {
        if (intensity <= 0)
            Owner.DisableEffect(type);
        else
            Owner.EnableEffect(type, (byte)Math.Min(intensity, byte.MaxValue));
    }

    private void ApplyFormEffect(Action action)
    {
        _isApplyingFormEffect = true;
        try
        {
            action();
        }
        finally
        {
            _isApplyingFormEffect = false;
        }
    }

    #endregion
}