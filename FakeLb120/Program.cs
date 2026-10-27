using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Hl7Lb120.Common;

namespace Hl7Lb120.FakeLb120
{
    // 장비 없이 시험하기 위한 가짜 LB120.. 나도 LB120...ㅜㅜㅜ
    // 오더는 리스너로 받고, 결과는 결과 수신 프로그램에 접속해서 보내기
    static class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("사용법: FakeLb120 <오더 대기 IP> <오더 대기 Port> <결과 보낼 IP> <결과 보낼 Port>");
                return;
            }
            var orderEndPoint = new IPEndPoint(IPAddress.Parse(args[0]), int.Parse(args[1]));
            var resultEndPoint = new IPEndPoint(IPAddress.Parse(args[2]), int.Parse(args[3]));
            TcpListener listener = new(orderEndPoint);

            // 수신할 때마다 1씩 증가
            int index = 0;

            try
            {
                listener.Start();
                Console.WriteLine($"오더 대기 중 : {orderEndPoint}");

                while (true)
                {
                    // 오더 보내는 쪽 접속 수락
                    using TcpClient handler = await listener.AcceptTcpClientAsync();
                    await using NetworkStream stream = handler.GetStream();
                    Console.WriteLine($"오더 전송 프로그램 접속 : {handler.Client.RemoteEndPoint}");

                    while (true)
                    {
                        // 오더 받기
                        var order = await Mllp.ReadFrameAsync(stream);
                        if (order == null)
                        {
                            Console.WriteLine("오더 연결이 끊어졌습니다.");
                            break;
                        }
                        index++;

                        // 오더에서 필요한 값 꺼내기
                        var segments = Hl7Parser.Parse(order);
                        var msh = segments.First(segment => segment.Name == "MSH");
                        var pid = segments.First(segment => segment.Name == "PID");
                        var heightObx = segments.First(segment => segment.Name == "OBX" && segment.GetComponent(3, 1) == "HT");

                        var patientId = pid.GetComponent(3, 1);
                        var familyName = pid.GetComponent(5, 1);
                        var givenName = pid.GetComponent(5, 2);
                        var birthDate = pid.GetField(7);
                        var sex = pid.GetField(8);
                        var height = heightObx.GetField(5);
                        var heightUnits = heightObx.GetField(6);

                        // 오더 출력
                        Console.WriteLine(
                            $"[오더] Index={index}, ID={patientId}, 이름={familyName}{givenName}, 성별={sex}, 생년월일={birthDate}, " +
                            $"신장={height} {heightUnits}, 메시지 고유 식별자={msh.GetField(10)}, 버전={msh.GetField(12)}");

                        // 랜덤 값으로 결과 생성
                        var result = Hl7Builder.BuildMessage(
                            Hl7Builder.BuildMsh("LB120", "FAKE", "HNA500UI", "HNA", "ORU^R01", $"FAKE-{DateTime.Now.Ticks}"),
                            Hl7Builder.BuildPid(patientId, familyName, givenName, birthDate, sex),
                            Hl7Builder.BuildObx(1, "NM", "HT", "Height", height, heightUnits, ""),
                            Hl7Builder.BuildObx(2, "NM", "DATETIMES", "Test Date / Time", RandomDateTime(), "", ""),
                            Hl7Builder.BuildObx(3, "NM", "WT", "Weight", RandomValue(45, 100), "kg", ""),
                            Hl7Builder.BuildObx(4, "NM", "BMI", "BMI (Body Mass Index)", RandomValue(15, 35), "", ""),
                            Hl7Builder.BuildObx(5, "NM", "PBF", "PBF (Percent Body Fat)", RandomValue(8, 40), "", ""));

                        // 결과 수신 프로그램에 접속해서 결과 보내기
                        await SendResultAsync(resultEndPoint, result);
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        // 결과 하나를 보낼 때마다 새로 접속하고 끊기
        static async Task SendResultAsync(IPEndPoint resultEndPoint, string result)
        {
            try
            {
                using TcpClient client = new();
                await client.ConnectAsync(resultEndPoint);
                await using NetworkStream stream = client.GetStream();

                await stream.WriteAsync(Mllp.Wrap(result));
                Console.WriteLine($"[결과 전송] {resultEndPoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"결과 전송 실패 : {ex.Message}");
            }
        }

        // min 이상 max 미만의 소수 첫째 자리 값
        static string RandomValue(int min, int max)
        {
            double value = min + Random.Shared.NextDouble() * (max - min);
            return value.ToString("F1", CultureInfo.InvariantCulture);
        }

        // 최근 30일 안의 랜덤 시각 (yyyyMMddHHmmss)
        static string RandomDateTime()
        {
            var secondsAgo = Random.Shared.Next(0, 30 * 24 * 60 * 60);
            return DateTime.Now.AddSeconds(-secondsAgo).ToString("yyyyMMddHHmmss");
        }
    }
}
