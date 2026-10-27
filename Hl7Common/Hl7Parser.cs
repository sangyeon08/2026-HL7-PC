namespace Hl7Lb120.Common
{
    // 문자열로 온 데이터를 나눠서 읽는 애.
    public class Hl7Segment
    {
        private readonly string[] _parts;

        public Hl7Segment(string line)
        {
            _parts = line.Split(Hl7Parser.FieldSeparator);
        }

        public string Name => _parts[0];

        public string GetField(int fieldNumber)
        {
            int index = fieldNumber;

            if (Name == "MSH")
            {
                if (fieldNumber == 1)
                {
                    return Hl7Parser.FieldSeparator.ToString();
                }
                index = fieldNumber - 1; // MSH만 예외적으로 | 를 포함해서 셈. 그래서 -1을 해주기.
            }
            return index < _parts.Length ? _parts[index] : "";
        }

        // 컴포넌트 꺼내기. 1번부터 시작
        public string GetComponent(int fieldNumber, int componentNumber)
        {
            var components = GetField(fieldNumber).Split(Hl7Parser.ComponentSeparator);
            return componentNumber <= components.Length ? components[componentNumber - 1] : "";
        }
    }

    public static class Hl7Parser
    {
        public const char SegmentTerminator = '\r';
        public const char FieldSeparator = '|';
        public const char ComponentSeparator = '^';

        // 세그먼트 단위로 자르기
        public static List<Hl7Segment> Parse(string body)
        {
            var segments = new List<Hl7Segment>();

            foreach (var line in body.Split(SegmentTerminator, StringSplitOptions.RemoveEmptyEntries))
            {
                segments.Add(new Hl7Segment(line));
            }
            return segments;
        }
    }
}
