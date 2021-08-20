using UnityEngine;
using UnityEngine.Video;
using System.Threading.Tasks;

public class Type4VideoPlay : MonoBehaviour
{
    public VideoPlayer videoPlayer; //ビデオ情報の格納
    public VideoClip[] videoClips;
    public bool randomMode = false; //trueのときランダムにビデオを再生

    public void PlayVideo(int num) // videoPlayer.isPreparedがfalseのときに呼び出してはいけない可能性
    {
        videoPlayer.Stop();
        videoPlayer.clip = videoClips[num];
        videoPlayer.Play();
    }
}
