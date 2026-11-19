using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Interfaces;

namespace RGM
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
        [Description("이것이 참일 시, 출력 시스템은 영어로 동작할 것입니다. (If true, the output will be in English.)")]
        public bool EN { get; set; } = false;
        [Description("여기에 특정 모드의 Enum을 입력하면 그 모드가 고정적으로 나옵니다. 랜덤게임모드 관련 기능들이 일부 차단됩니다.")]
        public List<ModeType> FixedModes { get; set; } = new();
        
        [Description("개발자 모드입니다. 일부 기능이 비활성화됩니다.")]
        public bool IsDevMode { get; set; } = false;

        [Description("제재 시간을 설정합니다. (HH:MM:SS, D.HH:MM:SS)")]
        public string BanTime { get; set; } = "00:30:00";

        [Description("서버의 시즌을 설정합니다.")] 
        public ushort Season { get; set; } = 2;
        
        [Description("플레이어의 시즌을 설정하지 않았을 때 시즌을 초기화를 허용합니다.")] 
        public bool ForceSeasonClear { get; set; } = false;
    }
}
