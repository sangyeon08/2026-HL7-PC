using System;
using System.Net;
using System.Net.Sockets;
using Hl7Lb120.Common;

namespace Hl7Lb120.Server
{
    static class Program
    {
        // 오더를 보낼 환자
        static readonly (string Id, string FamilyName, string GivenName, string Sex, string BirthDate, string Height)[] Patients =
        {
            ("10001", "홍", "길동", "M", "19900101", "175.0"),
            ("10002", "도", "우너", "F", "20080115", "160.1"),
            ("10003", "마", "이콜", "M", "20000101", "180.0"),
        };

        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("사용법: TcpServer <IP 주소> <Port 번호>");
                return;
            }
            var ipAddress = IPAddress.Parse(args[0]);
            var endPoint = new IPEndPoint(ipAddress, int.Parse(args[1]));
            TcpListener listener = new(endPoint);

            int index = 0;
            int orderNumber = 0;

            try
            {
                listener.Start();

                while (true)
                {
                    // 접속 수락
                    using TcpClient handler = await listener.AcceptTcpClientAsync();
                    await using NetworkStream stream = handler.GetStream();
                    Console.WriteLine("클라이언트 접속");

                    foreach (var patient in Patients) // 회원수만큼 반복
                    {
                        // 오더 전송
                        orderNumber++;
                        var order = Hl7Builder.BuildMessage(
                            Hl7Builder.BuildMsh("HNA500UI", "HNA", "", "", "ORM^O01", $"MSG{orderNumber:D5}"),
                            Hl7Builder.BuildPid(patient.Id, patient.FamilyName, patient.GivenName, patient.BirthDate, patient.Sex),
                            Hl7Builder.BuildObx(1, "ST", "HT", "HEIGHT", patient.Height, "cm", DateTime.Now.ToString("yyyyMMddHHmmss")));
                        await stream.WriteAsync(Mllp.Wrap(order));

                        // 결과 수신
                        var result = await Mllp.ReadFrameAsync(stream);
                        if (result == null)
                        {
                            Console.WriteLine("클라이언트 연결이 끊어졌습니다.");
                            break;
                        }
                        index++;

                        PrintResult(index, result);
                    }
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        static void PrintResult(int index, string result)
        {
            var segments = Hl7Parser.Parse(result);
            var pid = segments.First(segment => segment.Name == "PID");

            // OBX로 원하는 측정값 찾기
            Hl7Segment FindObx(string code) =>
                segments.First(segment => segment.Name == "OBX" && segment.GetComponent(3, 1) == code);

            var height = FindObx("HT");
            var testDateTime = FindObx("DATETIMES");
            var weight = FindObx("WT");
            var percentBodyFat = FindObx("PBF");
            var bmi = FindObx("BMI");

            Console.WriteLine(
                $"[결과] Index={index}, ID={pid.GetComponent(3, 1)}, 이름={pid.GetComponent(5, 1)}{pid.GetComponent(5, 2)}, 성별={pid.GetField(8)}, " +
                $"신장={height.GetField(5)} {height.GetField(6)}, 검사일시={testDateTime.GetField(5)}, " +
                $"체중={weight.GetField(5)} {weight.GetField(6)}, 체지방률={percentBodyFat.GetField(5)}%, BMI={bmi.GetField(5)}");
        }
    }
}
