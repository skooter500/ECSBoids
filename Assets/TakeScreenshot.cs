using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class TakeScreenshot : MonoBehaviour {

    public string path = "/Pictures/Infinite Forms";
    public string filePrefix = "Infinite Forms ";


    public string GenerateFileName()
    {
        string home = (Environment.OSVersion.Platform == PlatformID.Unix ||
                   Environment.OSVersion.Platform == PlatformID.MacOSX)
            ? Environment.GetEnvironmentVariable("HOME")
            : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

        DirectoryInfo info = new DirectoryInfo(home + path);
        if (!info.Exists)
        {
            Directory.CreateDirectory(home + path);
        }
        FileInfo[] fileInfo = info.GetFiles();
        int last = 0;
        foreach (FileInfo file in fileInfo)
        {
            if (file.Name.StartsWith(filePrefix))
            {
                string numPart = file.Name.Substring(filePrefix.Length
                    , 5);
                int fileNumber = int.Parse(numPart);
                if (fileNumber > last)
                {
                    last = fileNumber;
                }
            }            
        }
        return home + path + "/" + filePrefix + (last + 1).ToString("00000") + ".png";
    }

	void Start () {

        // See: https://stackoverflow.com/questions/1143706/getting-the-path-of-the-home-directory-in-c

        
    }
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            string filename = GenerateFileName();
            Debug.Log("Capturing screenshot to: " + filename);
            ScreenCapture.CaptureScreenshot(filename);
        }        
    }
}
