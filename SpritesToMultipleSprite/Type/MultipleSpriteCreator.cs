using UnityEngine;

namespace Momos.Tools.SpritesToMultipleSprite
{
    public class MultipleSpriteCreator : MonoBehaviour
    {
        public string outName = "Texture";
        public int padding = 2;
        [Header("要手动开启Read/Write")]
        public Sprite[] sprites;

        public string OutName => outName != null ? outName : "Texture";
    }
}