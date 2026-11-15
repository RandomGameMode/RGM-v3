using System.Collections.Generic;
using PlayerRoles;
using PlayerEventArgs = Exiled.Events.EventArgs.Player;

namespace RGM.Modes.PveExiledSystem.Difficulty
{
    public class Insane : WaveConfig
    {
        public override bool IsSpecial { get; } = false;
        public override int Difficulty { get; } = 2;
        public override string DifficultyName { get; } = "<color=\"red\">지옥</color>";

        public override void OnHurting(PlayerEventArgs.HurtingEventArgs ev)
        {
            if (ev.Player == null) return;
            if (ev.Attacker == null) return;
            if (!ev.Attacker.IsNPC) return;
            if (ev.Player.IsNPC)
            {
                ev.IsAllowed = false;
                return;
            }

            if (ev.Attacker.CurrentItem == null)
            {
                if (ev.Attacker.Role.Type == RoleTypeId.Scp939) ev.DamageHandler.Damage *= 1.5f;
                return;
            }

            switch (ev.Attacker.CurrentItem.Type)
            {
                case ItemType.SCP1509: ev.DamageHandler.Damage *= 0.5f; break;
                case ItemType.MicroHID: ev.DamageHandler.Damage *= 0.3f; break;
                case ItemType.GunLogicer: ev.DamageHandler.Damage *= 0.9f; break;
                case ItemType.ParticleDisruptor: ev.DamageHandler.Damage *= 0.5f; break;

                case ItemType.GunCrossvec: ev.DamageHandler.Damage *= 0.5f; break;
                case ItemType.GunA7: ev.DamageHandler.Damage *= 0.35f; break;
                case ItemType.GunAK: ev.DamageHandler.Damage *= 0.6f; break;
                default: ev.DamageHandler.Damage *= 0.6f; break;
            }
        }

        /*
       ClassD: 3, 30
       Gunner: 2, 24
       Scout: 2, 24
       Cloaker: 1, 4
       Zombie: 2, 16
       Rifleman: 1, 15
       Tranquilizer: 1, 6
       Pyromancer: 1, 5
       Striker: 1, 3
       Juggernaut: 1, 4
       Demolisher: 0, 0
       Sniper: 1, 7
       Assassin: 1, 1
        */

        public override WaveInfo[] Waves { get; } = new WaveInfo[] //웨이브 구조
        {
            new(
                intermissionTime: 10,
                bcText: "웨이브 1",
                supplySpawnInfos: new List<SupplySpawnInfo>
                {
                    new(ItemType.SCP1344, 1, 5),
                    new(ItemType.GunE11SR, 1, 3.8f),
                    new(ItemType.ArmorHeavy, 1, 5),
                    new(ItemType.Ammo556x45, 2, 7)
                },
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 12),
                    new("Zombie", 0.5f, 6)
                },
                supplyGiveInfos: new List<ItemType> { ItemType.Radio },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 15,
                minEnemyCount: 1,
                maxMinEnemyCount: 6
            ),
            new(
                intermissionTime: 10,
                bcText: "웨이브 2",
                supplySpawnInfos: new List<SupplySpawnInfo>(),
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 14),
                    new("Zombie", 1, 6),
                    new("Cloaker", 0.5f, 2)
                },
                supplyGiveInfos: new List<ItemType> { ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 15,
                minEnemyCount: 1,
                maxMinEnemyCount: 6
            ),
            new(
                intermissionTime: 10,
                bcText: "웨이브 3",
                supplySpawnInfos: new List<SupplySpawnInfo>
                {
                    new(ItemType.SCP1344, 0.8f, 5),
                    new(ItemType.AntiSCP207, 1, 5),
                    new(ItemType.SCP500, 2, 12),
                    new(ItemType.Medkit, 2, 8),
                    new(ItemType.SCP268, 1, 6)
                },
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 16),
                    new("Zombie", 1, 6),
                    new("Cloaker", 0.5f, 2)
                },
                supplyGiveInfos: new List<ItemType>
                    { ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 15,
                minEnemyCount: 1,
                maxMinEnemyCount: 6
            ),
            new(
                intermissionTime: 10,
                bcText: "웨이브 4",
                supplySpawnInfos: new List<SupplySpawnInfo>(),
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 16),
                    new("Scout", 0.5f, 8),
                    new("Zombie", 1, 6),
                    new("Cloaker", 1, 2),
                    new("Pyromancer", 0.5f, 1.5f)
                },
                supplyGiveInfos: new List<ItemType> { ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 15,
                minEnemyCount: 1,
                maxMinEnemyCount: 6
            ),
            new(
                intermissionTime: 20,
                bcText: "<color=\"red\">웨이브 5</color>",
                supplySpawnInfos: new List<SupplySpawnInfo>(),
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 12),
                    new("Scout", 1, 6),
                    new("Zombie", 1, 6),
                    new("Cloaker", 0.5f, 2),
                    new("Tranquilizer", 1, 3.5f),
                    new("Pyromancer", 1, 2.8f),
                    new("Striker", 1, 2.8f)
                },
                supplyGiveInfos: new List<ItemType> { ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 15,
                minEnemyCount: 1,
                maxMinEnemyCount: 6
            ),
            new(
                intermissionTime: 15,
                bcText: "웨이브 6",
                supplySpawnInfos: new List<SupplySpawnInfo>
                {
                    new(ItemType.SCP1853, 1, 8),
                    new(ItemType.Jailbird, 1, 4.8f),
                    new(ItemType.SCP500, 2, 12),
                    new(ItemType.SCP1853, 2, 8)
                },
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 14),
                    new("Scout", 1, 8),
                    new("Rifleman", 0.5f, 4),
                    new("Zombie", 1, 8),
                    new("Cloaker", 1, 2.8f),
                    new("Tranquilizer", 1, 3),
                    new("Pyromancer", 0.5f, 1.8f)
                },
                supplyGiveInfos: new List<ItemType>
                {
                    ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit,
                    ItemType.Radio, ItemType.Ammo556x45, ItemType.Ammo556x45, ItemType.GunCrossvec
                },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 20,
                minEnemyCount: 1,
                maxMinEnemyCount: 10
            ),
            new(
                intermissionTime: 15,
                bcText: "웨이브 7",
                supplySpawnInfos: new List<SupplySpawnInfo>(),
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 14),
                    new("Scout", 1, 8),
                    new("Rifleman", 0.5f, 8),
                    new("Zombie", 1, 10),
                    new("Cloaker", 1, 2.8f),
                    new("Tranquilizer", 1, 3),
                    new("Sniper", 0.5f, 2.8f),
                    new("Pyromancer", 0.5f, 1.8f)
                },
                supplyGiveInfos: new List<ItemType> { ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 20,
                minEnemyCount: 1,
                maxMinEnemyCount: 10
            ),
            new(
                intermissionTime: 15,
                bcText: "웨이브 8",
                supplySpawnInfos: new List<SupplySpawnInfo>(),
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 14),
                    new("Scout", 1, 10),
                    new("Rifleman", 1, 8),
                    new("Zombie", 1, 12),
                    new("Cloaker", 1, 2.8f),
                    new("Tranquilizer", 1, 3),
                    new("Sniper", 1, 5),
                    new("Pyromancer", 0.5f, 1.8f)
                },
                supplyGiveInfos: new List<ItemType> { ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 20,
                minEnemyCount: 1,
                maxMinEnemyCount: 10
            ),
            new(
                intermissionTime: 15,
                bcText: "웨이브 9",
                supplySpawnInfos: new List<SupplySpawnInfo>
                {
                    new(ItemType.GunSCP127, 1, 5),
                    new(ItemType.ArmorHeavy, 0.8f, 5),
                    new(ItemType.SCP500, 2, 12),
                    new(ItemType.Medkit, 2, 8),
                    new(ItemType.SCP268, 0.8f, 6)
                },
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 12),
                    new("Scout", 2, 16),
                    new("Rifleman", 1, 8),
                    new("Zombie", 1, 14),
                    new("Cloaker", 1, 2.8f),
                    new("Tranquilizer", 1, 3),
                    new("Sniper", 1, 5),
                    new("Pyromancer", 0.5f, 1.8f)
                },
                supplyGiveInfos: new List<ItemType>
                {
                    ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit,
                    ItemType.GunE11SR, ItemType.Ammo556x45, ItemType.Ammo556x45
                },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 20,
                minEnemyCount: 1,
                maxMinEnemyCount: 10
            ),
            new(
                intermissionTime: 20,
                bcText: "<color=\"red\">웨이브 10</color>",
                supplySpawnInfos: new List<SupplySpawnInfo>(),
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 10),
                    new("Scout", 1, 12),
                    new("Rifleman", 1, 8),
                    new("Zombie", 1, 12),
                    new("Cloaker", 0.5f, 2),
                    new("Tranquilizer", 1, 2.8f),
                    new("Sniper", 0.5f, 2.8f),
                    new("Pyromancer", 1, 2.8f),
                    new("Juggernaut", 1, 4.8f),
                    new("Assassin")
                },
                supplyGiveInfos: new List<ItemType> { ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 20,
                minEnemyCount: 1,
                maxMinEnemyCount: 10
            ),
            new(
                intermissionTime: 15,
                bcText: "웨이브 11",
                supplySpawnInfos: new List<SupplySpawnInfo>(),
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 6),
                    new("Scout", 2, 20),
                    new("Rifleman", 1, 10),
                    new("Zombie", 2, 14),
                    new("Cloaker", 0.5f, 2),
                    new("Tranquilizer", 1, 4),
                    new("Sniper", 1, 5),
                    new("Pyromancer", 1, 2.8f),
                    new("Assassin", 0.5f, 1.8f)
                },
                supplyGiveInfos: new List<ItemType> { ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 24,
                minEnemyCount: 1,
                maxMinEnemyCount: 14
            ),
            new(
                intermissionTime: 15,
                bcText: "웨이브 12",
                supplySpawnInfos: new List<SupplySpawnInfo>
                {
                    new(ItemType.GunLogicer, 1, 3),
                    new(ItemType.GunFRMG0, 1, 4),
                    new(ItemType.AntiSCP207, 1, 3.8f),
                    new(ItemType.GunSCP127, 0.8f, 3.8f),
                    new(ItemType.SCP500, 2, 12),
                    new(ItemType.Medkit, 2, 8)
                },
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 6),
                    new("Scout", 2, 20),
                    new("Rifleman", 1, 12),
                    new("Zombie", 2, 14),
                    new("Cloaker", 0.5f, 2),
                    new("Tranquilizer", 1, 4),
                    new("Sniper", 2, 7),
                    new("Pyromancer", 1, 2.8f),
                    new("Assassin", 0.5f, 1.8f)
                },
                supplyGiveInfos: new List<ItemType>
                {
                    ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit,
                    ItemType.Ammo556x45, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo762x39, ItemType.GunAK
                },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 24,
                minEnemyCount: 1,
                maxMinEnemyCount: 14
            ),
            new(
                intermissionTime: 15,
                bcText: "웨이브 13",
                supplySpawnInfos: new List<SupplySpawnInfo>(),
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 6),
                    new("Scout", 2, 20),
                    new("Rifleman", 1, 14),
                    new("Zombie", 2, 14),
                    new("Cloaker", 0.5f, 2),
                    new("Tranquilizer", 1, 4),
                    new("Sniper", 2, 7),
                    new("Pyromancer", 1, 2.8f),
                    new("Juggernaut", 0.5f, 1.8f),
                    new("Assassin", 0.5f, 1.8f)
                },
                supplyGiveInfos: new List<ItemType>
                    { ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit, ItemType.Ammo556x45, ItemType.Ammo556x45 },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 24,
                minEnemyCount: 1,
                maxMinEnemyCount: 14
            ),
            new(
                intermissionTime: 15,
                bcText: "웨이브 14",
                supplySpawnInfos: new List<SupplySpawnInfo>(),
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 6),
                    new("Scout", 2, 20),
                    new("Rifleman", 1, 16),
                    new("Zombie", 2, 14),
                    new("Cloaker", 0.5f, 2),
                    new("Tranquilizer", 1, 4),
                    new("Sniper", 2, 7),
                    new("Pyromancer", 1, 2.8f),
                    new("Juggernaut", 1, 2.8f),
                    new("Assassin", 0.5f, 1.8f)
                },
                supplyGiveInfos: new List<ItemType>
                    { ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit, ItemType.Ammo556x45, ItemType.Ammo556x45 },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 24,
                minEnemyCount: 1,
                maxMinEnemyCount: 14
            ),
            new(
                intermissionTime: 30,
                bcText: "<color=\"red\">마지막 웨이브</color>",
                supplySpawnInfos: new List<SupplySpawnInfo>
                {
                    new(ItemType.SCP1853, 1, 8),
                    new(ItemType.Medkit, 2, 10),
                    new(ItemType.Jailbird, 0.8f, 1.8f),
                    new(ItemType.SCP1344, 1, 5),
                    new(ItemType.ArmorHeavy, 1, 8)
                },
                enemySpawnInfos: new List<EnemySpawnInfo>
                {
                    new("Gunner", 1, 15),
                    new("Scout", 2, 22),
                    new("Rifleman", 2, 14),
                    new("Zombie", 2, 16),
                    new("Cloaker", 0.5f, 3),
                    new("Tranquilizer", 1, 5),
                    new("Sniper", 3, 8),
                    new("Pyromancer", 1, 3.8f),
                    new("Juggernaut", 1, 2.8f),
                    new("Assassin"),
                    new("Striker", 1, 3.6f)
                },
                supplyGiveInfos: new List<ItemType>
                {
                    ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Ammo9x19, ItemType.Medkit,
                    ItemType.Ammo556x45, ItemType.Ammo556x45, ItemType.Ammo762x39, ItemType.Ammo762x39, ItemType.Radio, ItemType.GunAK
                },
                maxEnemyCount: 5,
                maxMaxEnemyCount: 24,
                minEnemyCount: 1,
                maxMinEnemyCount: 14
            )
        };
    }
}