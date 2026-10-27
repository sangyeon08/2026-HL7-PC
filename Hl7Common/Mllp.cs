using System.Text;

namespace Hl7Lb120.Common
{
    // MLLP란, 통신 프로토콜인데, hl7을 tcp ip로 보낼 때 사용함
    public static class Mllp
    {
        public const byte StartBlock = 0x0B;
        public const byte EndBlock = 0x1C;
        public const byte CarriageReturn = 0x0D;
        // EB랑 CR 차이가 택배 보낼 때 내용물 다 담았다~ 랑 택배 포장 했다~ 차이 느낌.
        // EB는 프레임의 끝이고, CR은 줄바꿈

        // 본문을 감싸기
        public static byte[] Wrap(string body)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var frame = new byte[bodyBytes.Length + 3];

            frame[0] = StartBlock;
            bodyBytes.CopyTo(frame, 1);
            frame[^2] = EndBlock;
            frame[^1] = CarriageReturn;

            return frame;
        }

        // 스트림에서 바이트 하나씩 읽어서 데이터 다 얻기 (cancellationToken으로 기다림을 멈출 수 있음)
        public static async Task<string?> ReadFrameAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var oneByte = new byte[1];
            var body = new List<byte>();
            var started = false;

            while (true)
            {
                int read = await stream.ReadAsync(oneByte, cancellationToken);
                if (read == 0)
                {
                    return null;
                }
                byte b = oneByte[0];

                if (!started)
                {
                    // SB 전의 바이트는 버리기
                    if (b == StartBlock)
                    {
                        started = true;
                    }
                }
                else if (b == EndBlock)
                {
                    // EB 다음에 무조건 CR이 와야 프레임 완성
                    read = await stream.ReadAsync(oneByte, cancellationToken);
                    if (read == 0)
                    {
                        return null;
                    }
                    if (oneByte[0] != CarriageReturn)
                    {
                        throw new InvalidDataException("프레임 끝이 올바르지 않습니다.");
                    }
                    return Encoding.UTF8.GetString(body.ToArray());
                }
                else
                {
                    body.Add(b);
                }
            }
        }
    }
}
