using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace kmty.anim {
    [RequireComponent(typeof(AnimationController))]
    public class AnimationPlayer : MonoBehaviour {

        public List<AnimationPlaylistItem> playlist = new List<AnimationPlaylistItem>();
        public bool playOnStart = true;
        public bool isRepeatPlay = true;

        void Start() {
            var controller = GetComponent<AnimationController>();
            if (playOnStart == true) controller.Play(playlist.Where(p => p.isEnable == true).ToList(), false, isRepeatPlay);
        }

        public void SetPlaylist(List<AnimationPlaylistItem> playlist) {
            this.playlist = playlist;
        }
    }
}
