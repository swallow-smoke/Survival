namespace AstraNope.Types.States
{
    public enum PlayerVehicleState
    {
        None,         // 도보 (육지/수영)
        InsideLarge,  // 대형 잠수함 내부 보행 (좌석 미착석)
        Seated        // 조종 중 (소형/대형 공통)
    }
}