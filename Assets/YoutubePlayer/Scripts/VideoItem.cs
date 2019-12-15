using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using YoutubeExplode;
using YoutubeExplode.Models;

namespace YoutubePlayer
{
    [RequireComponent(typeof(Button))]
    public class VideoItem : MonoBehaviour
    {
        [SerializeField]
        private Image thumbnail = null;

        [SerializeField]
        private TextMeshProUGUI titleText = null;

        [SerializeField]
        private TextMeshProUGUI authorText = null;

        [SerializeField]
        private TextMeshProUGUI durationText = null;

        private Button button = null;
        private Video video = null;
        private VideosScrollView videosScroll;
        private YoutubePlayer player = null;

        private void Awake()
        {
            player = FindObjectOfType<YoutubePlayer>();

            button = GetComponent<Button>();
            button.onClick.AddListener(() => 
            {
                if (this.video == null)
                {
                    return;
                }

                player.PlayVideoByIdAsync(this.video.Id);
                this.videosScroll.ClearAllItems();
            });
        }

        public void Init(Video video, VideosScrollView videosScroll)
        {
            StartCoroutine(GetThumbnailCoroutine(video.Thumbnails.LowResUrl));

            this.titleText.text = video.Title;
            this.authorText.text = video.Author;
            this.durationText.text = video.Duration.ToString();

            this.video = video;
            this.videosScroll = videosScroll;
        }

        private IEnumerator GetThumbnailCoroutine(string uri)
        {
            using (UnityWebRequest webReq = UnityWebRequestTexture.GetTexture(uri))
            {
                yield return webReq.SendWebRequest();

                string[] pages = uri.Split('/');
                int page = pages.Length - 1;
                if (webReq.isNetworkError)
                {
                    Debug.LogError(pages[page] + ": Error: " + webReq.error);
                }
                else
                {
                    Debug.Log(pages[page] + ":\nReceived: " + webReq.downloadHandler.text);

                    var texture = DownloadHandlerTexture.GetContent(webReq);
                    Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    thumbnail.sprite = sprite;
                }
            }
            yield return null;
        }
    }
}