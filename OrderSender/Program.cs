using System.Net;
using System.Net.Sockets;
using Hl7Lb120.Common;

namespace Hl7Lb120.OrderSender
{
    // LB120의 리스너에 접속해서 오더 메시지를 보내기
    static class Program
    {
        // 오더를 보낼 환자
        static readonly (string Id, string FamilyName, string GivenName, string Sex, string BirthDate, string Height)[] Patients =
        {
            ("10001", "홍", "길동", "M", "19900101", "175.0"),
            ("10002", "도", "우너", "F", "20080115", "160.1"),
            ("10003", "마", "이콜", "M", "20000101", "180.0"),
        };

        static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(5);

        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("사용법: OrderSender <LB120 IP 주소> <Port 번호>");
                return;
            }
            var ipAddress = IPAddress.Parse(args[0]);
            var ipEndPoint = new IPEndPoint(ipAddress, int.Parse(args[1]));

            // 보낼 때마다 1씩 증가
            int orderNumber = 0;

            try
            {
                // 장비 리스너에 접속
                using TcpClient client = new();
                await client.ConnectAsync(ipEndPoint);
                await using NetworkStream stream = client.GetStream();
                Console.WriteLine($"장비 접속 : {ipEndPoint}");

                foreach (var patient in Patients)
                {
                    // 오더 전송
                    orderNumber++;
                    var order = Hl7Builder.BuildMessage(
                        Hl7Builder.BuildMsh("HNA500UI", "HNA", "", "", "ORM^O01", $"MSG{orderNumber:D5}"),
                        Hl7Builder.BuildPid(patient.Id, patient.FamilyName, patient.GivenName, patient.BirthDate, patient.Sex),
                        Hl7Builder.BuildObx(1, "ST", "HT", "HEIGHT", patient.Height, "cm", DateTime.Now.ToString("yyyyMMddHHmmss")));
                    await stream.WriteAsync(Mllp.Wrap(order));
                    Console.WriteLine($"[오더 전송] ID={patient.Id}, 이름={patient.FamilyName}{patient.GivenName}, 메시지 고유 식별자=MSG{orderNumber:D5}");

                    // 장비가 응답을 보내는지 잠시 기다려 보기
                    using var timeout = new CancellationTokenSource(ReplyTimeout);
                    try
                    {
                        var reply = await Mllp.ReadFrameAsync(stream, timeout.Token);
                        if (reply == null)
                        {
                            Console.WriteLine("장비 연결이 끊어졌습니다.");
                            break;
                        }
                        Console.WriteLine("[응답]");
                        Console.WriteLine(reply.Replace(Hl7Parser.SegmentTerminator, '\n'));
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine($"[응답 없음] {ReplyTimeout.TotalSeconds}초 동안 응답이 없었습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"장비 연결 실패 : {ex.Message}");
            }
        }
    }
}
