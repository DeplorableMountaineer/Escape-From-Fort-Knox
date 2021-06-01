/*
 *           ~~ Screenshot Utility ~~ 
 *  Takes a screenshot of the game window with its
 *  current resolution. Should work on any platform
 *  or the editor.
 * 
 *  Notes:
 *    - Images are stored in a Screenshots folder within the Unity project directory.
 * 
 *    - Images will copied over if player prefs are reset!
 * 
 *    - If the resolution is 1024x768, and the scale factor
 *      is 2, the screenshot will be saved as 2048x1536.
 * 
 *    - The mouse is not captured in the screenshot.
 * 
 *  Michigan State University
 *  Games for Entertainment and Learning (GEL) Lab
 */

//Modifications by TDV

using System.IO;
using JetBrains.Annotations;
using UnityEngine;

// included for access to File IO such as Directory class

namespace Deplorable_Mountaineer {
    /// <summary>
    ///     Handles taking a screenshot of game window.
    /// </summary>
    public class ScreenshotUtility : MonoBehaviour {
        #region Constants

        // The key used to get/set the number of images
        private const string ImageCntKey = "IMAGE_CNT";

        #endregion

        // static reference to ScreenshotUtility so can be called from other scripts directly (not just through GameObject component)
        [PublicAPI] public static ScreenshotUtility ScreenShotUtility;

        #region Private Variables

        // The number of screenshots taken
        private int _imageCount;

        #endregion

        /// <summary>
        ///     Lets the screenshot utility persist through scenes.
        /// </summary>
        private void Awake(){
            if(ScreenShotUtility != null){
                // this GameObject must already have been setup in a previous scene, so just destroy this game object
                Destroy(gameObject);
            }
            else{
                // this is the first time we are setting up the screenshot utility
                // setup reference to ScreenshotUtility class
                ScreenShotUtility = GetComponent<ScreenshotUtility>();

                // keep this GameObject around as new scenes load
                DontDestroyOnLoad(gameObject);

                // get image count from player prefs for indexing of filename
                _imageCount = PlayerPrefs.GetInt(ImageCntKey);
            }

            // if there is not a "Screenshots" directory in the Project folder, create one
            if(!Directory.Exists("Screenshots")) Directory.CreateDirectory("Screenshots");
        }

        /// <summary>
        ///     Called once per frame. Handles the input.
        /// </summary>
        private void Update(){
            // Checks for input
            if(Input.GetKeyDown(screenshotKey)){
                // Saves the current image count
                PlayerPrefs.SetInt(ImageCntKey, ++_imageCount);

                // Adjusts the height and width for the file name
                int width = Screen.width*scaleFactor;
                int height = Screen.height*scaleFactor;

                // Takes the screenshot with filename "Screenshot_WidthXHeight_ImageCount.png"
                // and save it in the Screenshots folder
                ScreenCapture.CaptureScreenshot("Screenshots/Screenshot_" +
                                                +width + "x" + height
                                                + "_"
                                                + _imageCount
                                                + ".png",
                    scaleFactor);
            }
        }

        #region Public Variables

        // The key used to take a screenshot
        public KeyCode screenshotKey = KeyCode.F9;

        // The amount to scale the screenshot
        public int scaleFactor = 1;

        #endregion
    }
}