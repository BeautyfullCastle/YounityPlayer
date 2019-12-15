using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YoutubeExplode.Models;

namespace YoutubePlayer
{
    public class VideosScrollView : MonoBehaviour
    {
        [SerializeField]
        private VideoItem item = null;

        [SerializeField]
        private YoutubeSearcher searcher = null;

        private ScrollRect scroll = null;
        private List<VideoItem> items = null;

        private void Awake()
        {
            scroll = GetComponent<ScrollRect>();
        }

        private void OnEnable()
        {
            searcher.OnSearched += OnSearched;
        }

        private void OnDisable()
        {
            searcher.OnSearched -= OnSearched;
        }

        private void OnSearched(IReadOnlyList<Video> videos)
        {
            ClearAllItems();

            foreach (var video in videos)
            {
                var videoItem = Instantiate(item, scroll.content) as VideoItem;
                videoItem.Init(video, this);
                items.Add(videoItem);
            }

            this.WaitForNextFrame(()=> this.scroll.normalizedPosition = new Vector2(0f, 1f));
        }

        public void ClearAllItems()
        {
            if (items == null)
            {
                items = new List<VideoItem>();
            }
            else if(items.Count > 0)
            {
                items.ForEach((x) => Destroy(x.gameObject));
                items.Clear();
            }
        }
    }
}