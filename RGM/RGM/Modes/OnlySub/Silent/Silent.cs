using Exiled.API.Features.Items;
using System.Linq;
using Exiled.Events.EventArgs.Player;
using static RGM.Variables.Variable;
using VoiceChat.Codec;

namespace RGM.Modes
{
    [Mode(ModeCategory.OnlySub, ModeInfo.Plus, ModeType.Silent)]
    public class Silent : Mode
    {
        public override string Name => "사일런트";
        public override string Description => "쉿! 조용히 이야기하세요!";

        public override string Detail =>
            """
            조용히 이야기해야 합니다. (0.4보다 작게)
            채팅(.ㅊ)은 사용할 수 없습니다.
            """;

        public override string Color => "9E82F5";

        public static Silent Instance;

        private OpusDecoder _decoder;
        private float[] _decodedBuffer;

        public override void OnEnabled()
        {
            _decoder = new OpusDecoder();
            _decodedBuffer = new float[24000];

            Exiled.Events.Handlers.Player.VoiceChatting += OnVoiceChatting;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.VoiceChatting -= OnVoiceChatting;
        }

        private float GetLoudness(byte[] encodedBuffer, int length)
        {
            var len = _decoder.Decode(encodedBuffer, length, _decodedBuffer);

            return len < 0 ? 0f : _decodedBuffer.Max();
        }

        private void OnVoiceChatting(VoiceChattingEventArgs ev)
        {
            if (!ev.Player.IsAlive) return;
            if (!(GetLoudness(ev.VoiceMessage.Data, ev.VoiceMessage.DataLength) > 0.35f)) return;
            var g = (ExplosiveGrenade)Item.Create(ItemType.GrenadeHE, ev.Player);
            g.FuseTime = 0f;
            g.MaxRadius = 0;
            g.SpawnActive(ev.Player.Position, null);

            if (GodModePlayers.Contains(ev.Player))
                GodModePlayers.Remove(ev.Player);

            ev.Player.Kill("입이 근질거리는 것을 참지 못했습니다.");
        }
    }
}
