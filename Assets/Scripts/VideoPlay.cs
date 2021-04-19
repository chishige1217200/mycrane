using UnityEngine;
using UnityEngine.Video;
using System.Threading.Tasks;

public class VideoPlay : MonoBehaviour
{
    public VideoPlayer[] videoPlayer; //オーディオ情報の格納
    public VideoClip[] videoClips;
    public bool randomMode = false; //trueのときランダムにビデオを再生
    bool videoPlayFlag = false;
    float playTime = 0; //ビデオ再生時間
    float startTime;

    void Update()
    {
        if (randomMode)
        {
            if (playTime - Time.time + startTime < 0)
            {
                playTime = Random.Range(3, 8);
                startTime = Time.time;
                PlayVideo(Random.Range(0, videoClips.Length));
            }
        }

        if (videoPlayer[0].isPrepared && videoPlayFlag)
            videoPlayer[0].Play();
        if (videoPlayer[1].isPrepared && videoPlayFlag)
            videoPlayer[1].Play();

    }

    public void PlayVideo(int num)
    {
        videoPlayFlag = false;
        videoPlayer[0].clip = videoClips[num];
        videoPlayer[1].clip = videoClips[num];
        videoPlayer[0].Prepare();
        videoPlayer[1].Prepare();
        videoPlayFlag = true;
    }
}
