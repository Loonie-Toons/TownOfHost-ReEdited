using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public class ErrorText : MonoBehaviour
    {
        #region Singleton
        public static ErrorText Instance
        {
            get
            {
                return _instance;
            }
        }
        private static ErrorText _instance;
        private void Awake()
        {
            if (_instance != null)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
                DontDestroyOnLoad(this);
            }
        }
        #endregion
        public static void Create(TMPro.TextMeshPro baseText)
        {
            var Text = Instantiate(baseText);
            var instance = Text.gameObject.AddComponent<ErrorText>();
            instance.Text = Text;
            instance.name = "ErrorText";

            Text.enabled = false;
            Text.text = "NO ERROR";
            Text.color = Color.red;
            Text.outlineColor = Color.black;
            Text.alignment = TMPro.TextAlignmentOptions.Top;
        }

        public TMPro.TextMeshPro Text;
        public Camera Camera;
        public Vector3 TextOffset = new(0, 0.3f, -1000f);
        public void LateUpdate()
        {
            if (!Text.enabled) return;

            if (Camera == null)
                Camera = !HudManager.InstanceExists ? Camera.main : HudManager.Instance.PlayerCam.GetComponent<Camera>();
            if (Camera != null)
            {
                transform.position = AspectPosition.ComputeWorldPosition(Camera, AspectPosition.EdgeAlignments.Top, TextOffset);
            }
        }
    }
    public enum ErrorCode
    {
        //xxxyyyz: ERR-xxx-yyy-z
        //  xxx: エラー大まかなの種類 (HUD関連, 追放処理関連など)
        //  yyy: エラーの詳細な種類 (BoutyHunterの処理, SerialKillerの処理など)
        //  z:   深刻度
        //    0: 処置不要 (非表示)
        //    1: 正常に動作しなければ廃村 (一定時間で非表示)
        //    2: 廃村を推奨 (廃村で非表示)
        NoError = 0000000, // 000-000-0 No Error
    }
}