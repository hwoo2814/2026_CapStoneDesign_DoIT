using UnityEngine;
using System.Collections.Generic;

public class SuddenEventManager : MonoBehaviour
{
    public static SuddenEventManager Instance;

    public class EventData
    {
        public string eventName; // 이벤트 이름
        //public Sprite background; // 배경(앵커, 타이틀 배경 등등...)
        //public Sprite eventSprite; // 각 이벤트 이미지(앵커 옆에)

        // 이벤트별 이미지 설정
        /*public Sprite imgAiCompute;
        public Sprite imgSilverCare;
        public Sprite imgTechCollab;
        public Sprite imgEduTech;
        public Sprite imgStartup;
        public Sprite imgJobFear;
        public Sprite imgFraud;
        public Sprite imgFire;
        public Sprite imgWaterShock;
        public Sprite imgRegulation;*/

        public float dYouth, dSenior, dCorp; // 변경될 민심들
        public float dUniv, dSilver, dInd, dHouse; // 변경될 지역들
    }

    public float eventTriggerChance = 40f; // 이벤트 발생 확률, 임시로 정함
    
    public List<EventData> events; // 이벤트 목록

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitEventData(); // 게임 시작 시 events 리스트에 있는 이벤트를 등록
    }

    // 이벤트 발생 함수
    public void CheckAndTriggerEvent()
    {
        // 이벤트 발생 확률 추첨
        if (Random.Range(1f, 100f) > eventTriggerChance) return; 

        // 무작위 이벤트 1개 추첨
        int idx = Random.Range(0, events.Count);
        EventData ev = events[idx];

        // 이벤트 수치 적용
        ScoreManager.Instance.ModifyAffinity(ev.dYouth, ev.dSenior, ev.dCorp);
        ScoreManager.Instance.ModifyDev(ev.dUniv, ev.dSilver, ev.dInd, ev.dHouse);

        // 돌발 이벤트 UI 띄우기 (턴 시작 전 표출)
        UIManager.Instance.ShowEventPopup(ev.eventName/*, ev.background , ev.eventSprite*/);
    }

    private void InitEventData()
    {
        events = new List<EventData>()
        {
            // 긍정 이벤트 5개
            new EventData { 
                eventName = "AI 컴퓨팅, 데이터 센터 유치",
                dInd = 15f, dHouse = -5f, dCorp = 0.6f 
            },
            new EventData { 
                eventName = "실버 AI 돌봄 서비스 보급", 
                dSilver = 10f, dInd = 15f, dSenior = 0.4f 
            },
            new EventData { 
                eventName = "글로벌 빅테크와 국내 대학 협력", 
                dUniv = 5f, dInd = 7f, dYouth = 0.8f 
            },
            new EventData { 
                eventName = "AI 에듀테크 시범지구 선정", 
                dUniv = 14f, dYouth = 0.8f 
            },
            new EventData { 
                eventName = "K-AI 스타트업 유니콘 탄생", 
                dInd = 12f 
            },

            // 부정 이벤트 5개
            new EventData { 
                eventName = "AI 일자리 대체 공포 확산", 
                dYouth = -0.7f, dSenior = -0.4f 
            },
            new EventData { 
                eventName = "AI 금융 사기 급증", 
                dYouth = -0.3f, dSenior = -0.3f, dCorp = -0.3f 
            },
            new EventData { 
                eventName = "데이터 센터 전력 과부하, 화재 사고", 
                dInd = -15f 
            },
            new EventData { 
                eventName = "AI 연산용 '워터 쇼크'", 
                dHouse = -15f, dYouth = -0.5f, dSenior = -0.5f, dCorp = -0.5f 
            },
            new EventData { 
                eventName = "글로벌 AI 규제 요구", 
                dCorp = -0.7f, dInd = -9f 
            }
        };
    }
}