using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public GameObject startPopupPanel; // 튜토리얼인지 바로시작인지 물어보는 팝업창

    // 타이틀 화면에서 재생할 BGM AudioSource들
    public AudioSource titleBgmAudioSource1;
    public AudioSource titleBgmAudioSource2;
    public AudioSource titleBgmAudioSource3;
    public AudioSource titleBgmAudioSource4;

    // 랜덤 BGM 코루틴 중복 실행 방지용
    private Coroutine titleBgmRandomPlayCoroutine;

    // 현재 재생 중인 타이틀 BGM 확인용
    private AudioSource currentTitleBgmAudioSource;

    void Start()
    {
        // 씬이 시작될 때 팝업창은 화면에서 끔
        if (startPopupPanel != null)
            startPopupPanel.SetActive(false);

        // 타이틀 화면 BGM 시작
        StartRandomTitleBGM();
    }

    // 메인 화면의 Game Start 버튼을 누르면 팝업창을 띄우는 함수
    public void OpenStartPopup()
    {
        startPopupPanel.SetActive(true);
    }

    // 팝업창의 튜토리얼 버튼을 눌렀을 때 실행될 함수
    public void PlayWithTutorial()
    {
        // 씬 전환 전에 타이틀 BGM 정지
        StopRandomTitleBGM();

        // PlayTutorial에 1을 적고 게임 씬으로
        PlayerPrefs.SetInt("PlayTutorial", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }

    // 팝업창의 바로 시작 버튼을 눌렀을 때 실행될 함수
    public void PlayWithoutTutorial()
    {
        // 씬 전환 전에 타이틀 BGM 정지
        StopRandomTitleBGM();

        // PlayTutorial에 0을 적고 게임 씬으로
        PlayerPrefs.SetInt("PlayTutorial", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameScene");
    }

    // 타이틀 랜덤 BGM 재생 시작
    public void StartRandomTitleBGM()
    {
        if (titleBgmRandomPlayCoroutine != null) return;

        titleBgmRandomPlayCoroutine = StartCoroutine(RandomTitleBGMCoroutine());
    }

    // 타이틀 랜덤 BGM 재생 정지
    public void StopRandomTitleBGM()
    {
        if (titleBgmRandomPlayCoroutine != null)
        {
            StopCoroutine(titleBgmRandomPlayCoroutine);
            titleBgmRandomPlayCoroutine = null;
        }

        StopAllTitleBGM();
    }

    // 타이틀 랜덤 BGM 반복 재생 코루틴
    private IEnumerator RandomTitleBGMCoroutine()
    {
        while (true)
        {
            AudioSource selectedBgm = GetRandomTitleBGMAudioSource();

            if (selectedBgm == null)
            {
                yield return null;
                continue;
            }

            StopAllTitleBGM();

            currentTitleBgmAudioSource = selectedBgm;

            // GameManager에서 쓰는 BGMVolume 값을 그대로 사용
            float savedVolume = PlayerPrefs.GetFloat("BGMVolume", currentTitleBgmAudioSource.volume);
            currentTitleBgmAudioSource.volume = savedVolume;

            currentTitleBgmAudioSource.Play();

            while (currentTitleBgmAudioSource != null && currentTitleBgmAudioSource.isPlaying)
            {
                yield return null;
            }
        }
    }

    // 등록된 AudioSource 중 하나를 랜덤 선택
    private AudioSource GetRandomTitleBGMAudioSource()
    {
        List<AudioSource> bgmList = new List<AudioSource>();

        if (titleBgmAudioSource1 != null) bgmList.Add(titleBgmAudioSource1);
        if (titleBgmAudioSource2 != null) bgmList.Add(titleBgmAudioSource2);
        if (titleBgmAudioSource3 != null) bgmList.Add(titleBgmAudioSource3);
        if (titleBgmAudioSource4 != null) bgmList.Add(titleBgmAudioSource4);

        if (bgmList.Count == 0) return null;

        int randomIndex = Random.Range(0, bgmList.Count);
        return bgmList[randomIndex];
    }

    // 타이틀 BGM 전체 정지
    private void StopAllTitleBGM()
    {
        if (titleBgmAudioSource1 != null) titleBgmAudioSource1.Stop();
        if (titleBgmAudioSource2 != null) titleBgmAudioSource2.Stop();
        if (titleBgmAudioSource3 != null) titleBgmAudioSource3.Stop();
        if (titleBgmAudioSource4 != null) titleBgmAudioSource4.Stop();
    }

    private void OnDisable()
    {
        // 씬 전환이나 오브젝트 비활성화 시 타이틀 BGM 정리
        StopRandomTitleBGM();
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때 혹시 남아 있을 수 있는 AudioSource 정리
        StopAllTitleBGM();
    }

    // 게임 끄기 버튼
    public void GameEndButton()
    {
        Application.Quit();
    }
}