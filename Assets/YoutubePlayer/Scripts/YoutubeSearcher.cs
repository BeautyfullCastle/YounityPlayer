using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YoutubeExplode;
using YoutubeExplode.Models;

namespace YoutubePlayer
{
    [RequireComponent(typeof(TMPro.TMP_InputField))]
    public class YoutubeSearcher : MonoBehaviour
    {
        private TMPro.TMP_InputField field = null;

        private YoutubeClient youtubeClient = null;

        public IReadOnlyList<Video> Results { get; private set; } = null;
        public Action<IReadOnlyList<Video>> OnSearched = null;

        private void Awake()
        {
            field = GetComponent<TMPro.TMP_InputField>();

            youtubeClient = new YoutubeClient();

            field.onEndEdit.AddListener(async (input) =>
            {
                Results = await youtubeClient.SearchVideosAsync(input, 1);
                foreach (var item in Results)
                {
                    Debug.Log(item);
                }

                if (Results != null && Results.Count > 0)
                {
                    OnSearched?.Invoke(Results);
                }
            });
        }
    }
}