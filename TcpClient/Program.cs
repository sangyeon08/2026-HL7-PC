using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Hl7Lb120.Common;

namespace Hl7Lb120.Client
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("사용법: TcpClient <IP 주소> <Port 번호>");
                return;
            }
            var ipAddress = IPAddress.Parse(args[0]);
            var ipEndPoint = new IPEndPoint(ipAddress, int.Parse(args[1]));

            // 수신할 때마다 1씩 증가
            int index = 0;

            try
            {
                // 접속
                using TcpClient client = new();
                await client.ConnectAsync(ipEndPoint);
                await using NetworkStream stream = client.GetStream();
                Console.WriteLine($"서버 접속");

                while (true)
                {
                    // 오더 받기
                    var order = await Mllp.ReadFrameAsync(stream);
                    if (order == null)
                    {
                        Console.WriteLine("서버 연결이 끊어졌습니다.");
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

                    // 전송
                    await stream.WriteAsync(Mllp.Wrap(result));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 연결 실패 : " + $"{ex.Message}");
                return;
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
