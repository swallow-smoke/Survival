namespace AstraNope.Data.Items.Types
{
    public enum ModifierType
    {
        Weight, // 무게 (모든 아이템)
        MaxStack, // 스택 한도
        Damage, // 무기 공격력
        HarvestRate, // 채집 도구 효율
        DurabilityMax, // 내구도 최대값
        ArmorValue, // 방어력
        ThermalInsulation, // 체온 보호 (방한 갑옷)
        HealAmount, // 회복량 (소모품)
        OxygenAmount, // 산소 (소모품)
        FoodValue, // 포만감
        WaterValue, // 수분
        ExplosivePower, // 폭발력 (폭탄)
        AmmoCapacity, // 탄창 (총기)
        ScanRange // 스캔 범위 (스캔 도구)
    }
}