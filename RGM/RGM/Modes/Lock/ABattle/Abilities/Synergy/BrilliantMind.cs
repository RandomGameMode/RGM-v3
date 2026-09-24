namespace RGM.Modes.Abilities.Synergy;

[RequiresAbility(AbilityType.DUMMY_TESTSUCCESS, AbilityType.DUMMY_FINALEXAMSUCCESS, AbilityType.DUMMY_CSATSUCCESS)]
[Ability("수재", "<시험류 3종 모두 성공> 님 혹시 천재?",
    AbilityCategory.Synergy, AbilityType.SYNERGY_BRILLIANTMIND)]

public class BrilliantMind : Ability;