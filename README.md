# 2026-HL7-PC

HL7 V2(MLLP over TCP) 오더 송신 / 결과 수신 예제 (.NET 8)

## LB120 HL7 V2 연동

## 빌드

.NET 8 SDK

```bash
dotnet build hl7-lb120.sln
```

## 설정

### 프로그램 인자

| 프로그램       | 역할                      | 인자                                                              |
| -------------- | ------------------------- | ----------------------------------------------------------------- |
| ResultReceiver | LB120의 검사 결과 수신    | `<대기할 IP> <Port>`                                              |
| OrderSender    | LB120으로 오더 전송       | `<LB120 PC의 IP> <LB120 오더 Port>`                               |
| FakeLb120      | LB120 없이 시험할 때 사용 | `<오더 대기 IP> <오더 대기 Port> <결과 보낼 IP> <결과 보낼 Port>` |

### LB120 설정

| 메뉴                   | 항목        | 값                              |
| ---------------------- | ----------- | ------------------------------- |
| 01. 오더 정보 연동     | 방식        | HL7 V2                          |
|                        | 리스너 포트 | 예: 5001                        |
| 02. 검사 데이터 연동 | 방식        | HL7 V2                          |
|                        | 전송 타입   | TCP/IP                          |
|                        | 서버 IP     | ResultReceiver를 실행한 PC의 IP |
|                        | 포트        | 예: 2575                        |
|                        | 자동 전송   | 사용                            |

연동하는 두 PC는 같은 네트워크 환경

## 구동 순서 (LB120 연동)

1. 결과 수신 프로그램 실행

```bash
dotnet run --project ResultReceiver -- 0.0.0.0 2575
```

2. 다른 터미널에서 오더를 전송

```bash
dotnet run --project OrderSender -- <LB120 PC의 IP> 5001
```

3. LB120 회원 목록에 전송한 회원이 표시되는지 확인
4. LB120에서 해당 회원을 선택하고 체성분 검사를 진행
5. 검사가 끝나면 ResultReceiver에 `[결과]`가 출력

## 구동 순서 (LB120 없이 시험)

1. 결과 수신 프로그램 실행

```bash
dotnet run --project ResultReceiver -- 0.0.0.0 2575
```

2. 다른 터미널에서 가짜 LB120을 실행

```bash
dotnet run --project FakeLb120 -- 0.0.0.0 5000 127.0.0.1 2575
```

3. 또 다른 터미널에서 오더를 전송

```bash
dotnet run --project OrderSender -- 127.0.0.1 5000
```

4. FakeLb120에 `[오더]`, ResultReceiver에 `[결과]`가 출력
