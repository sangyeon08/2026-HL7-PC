using System.Net;
using System.Net.Sockets;
using Hl7Lb120.Common;

namespace Hl7Lb120.ResultReceiver
{
    // LB120이 보내는 검사 결과를 기다렸다가 받기
    static class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("사용법: ResultReceiver <IP 주소> <Port 번호>");
                return;
            }
            var ipAddress = IPAddress.Parse(args[0]);
            var endPoint = new IPEndPoint(ipAddress, int.Parse(args[1]));
            TcpListener listener = new(endPoint);

            // 수신할 때마다 1씩 증가
            int index = 0;

            try
            {
                listener.Start();
                Console.WriteLine($"결과 대기 중 : {endPoint}");

                while (true)
                {
                    // 장비 접속 수락
                    using TcpClient handler = await listener.AcceptTcpClientAsync();
                    await using NetworkStream stream = handler.GetStream();
                    Console.WriteLine($"장비 접속 : {handler.Client.RemoteEndPoint}");

                    try
                    {
                        // 장비가 연결을 끊을 때까지 결과 받기
                        while (true)
                        {
                            var result = await Mllp.ReadFrameAsync(stream);
                            if (result == null)
                            {
                                Console.WriteLine("장비 연결이 끊어졌습니다.");
                                break;
                            }
                            index++;

                            Console.WriteLine($"[원문] Index={index}");
                            Console.WriteLine(result.Replace(Hl7Parser.SegmentTerminator, '\n'));

                            PrintResult(index, result);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 한 번의 접속에서 오류가 나도 다음 접속은 계속 기다리기
                        Console.WriteLine($"수신 오류 : {ex.Message}");
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
            var pid = segments.FirstOrDefault(segment => segment.Name == "PID");

            // OBX로 원하는 측정값 찾기
            string FindObx(string code)
            {
                var obx = segments.FirstOrDefault(segment => segment.Name == "OBX" && segment.GetComponent(3, 1) == code);
                if (obx == null)
                {
                    return "없음";
                }
                return $"{obx.GetField(5)} {obx.GetField(6)}".Trim();
            }

            Console.WriteLine(
                $"[결과] Index={index}, ID={pid?.GetComponent(3, 1)}, 이름={pid?.GetComponent(5, 1)}{pid?.GetComponent(5, 2)}, 성별={pid?.GetField(8)}, " +
                $"신장={FindObx("HT")}, 검사일시={FindObx("DATETIMES")}, " +
                $"체중={FindObx("WT")}, 체지방률(%)={FindObx("PBF")}, BMI={FindObx("BMI")}");
        }
    }
}
