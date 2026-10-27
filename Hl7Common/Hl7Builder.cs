namespace Hl7Lb120.Common
{
    public static class Hl7Builder
    {
        // MSH는 메시지 헤더
        public static string BuildMsh(string sendingApp, string sendingFacility, string receivingApp, string receivingFacility, string messageType, string controlId)
        {
            return string.Join(Hl7Parser.FieldSeparator,
                "MSH",
                @"^~\&", // 인코딩 문자
                sendingApp,
                sendingFacility,
                receivingApp,
                receivingFacility,
                DateTime.Now.ToString("yyyyMMddHHmmss"),
                "",
                messageType,
                controlId, // 메시지 고유 식별자
                "P", // 처리 모드
                "2.3",
                "", "", "", "", "",
                "UNICODE UTF-8");
        }

        // PID는 환자 정보
        public static string BuildPid(string patientId, string familyName, string givenName, string birthDate, string sex)
        {
            return string.Join(Hl7Parser.FieldSeparator,
                "PID",
                "1",
                patientId,
                patientId,
                "",
                familyName + Hl7Parser.ComponentSeparator + givenName,
                "",
                birthDate,
                sex,
                "", "", "", "", "", "", "", "");
        }

        // OBX는 측정값 한 건
        public static string BuildObx(int setId, string valueType, string code, string name, string value, string units, string observationDateTime)
        {
            return string.Join(Hl7Parser.FieldSeparator,
                "OBX",
                setId.ToString(), // 순번
                valueType, // 값 타입
                code + Hl7Parser.ComponentSeparator + name, // 측정 항목
                "",
                value,
                units, // 단위
                "", "", "", "",
                "F", // 결과 상태
                "", "",
                observationDateTime);
        }

        // 세그먼트들을 이어 본문 만들기
        public static string BuildMessage(params string[] segments)
        {
            return string.Join(Hl7Parser.SegmentTerminator, segments) + Hl7Parser.SegmentTerminator;
        }
    }
}
