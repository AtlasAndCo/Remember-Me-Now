using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class RememberMeNowScript : MonoBehaviour
{
    [SerializeField] private KMBombInfo bombRef;
    [SerializeField] private KMBombModule bombModuleRef;
    [SerializeField] private KMAudio audioRef;

    [SerializeField] private KMSelectable[] buttons;
    [SerializeField] private TextMesh displayText;
    [SerializeField] private TextMesh timerText;
    [SerializeField] private GameObject[] ledArray;
    [SerializeField] private Material[] lightMaterials;
    [SerializeField] private GameObject[] colourblindText;
    [SerializeField] private Light[] allLights;
    private string[] colourStrings = new string[] {"Red", "Blue", "Yellow", "Green", "Orange", "Purple", "Off"};
    private string[] lightColours = new string[] {"Off", "Off"};
    private Color[] colours = new Color[] {Color.red, Color.blue, Color.yellow, Color.green, new Color(1f, 0.6f, 0f), new Color(1f, 0f, 1f)};

    private List<int> acceptedNumbers = new List<int>();
    private List<int> rejectedNumbers = new List<int>();

    private Coroutine timerRef;

    private int stage = 1;
    private bool isValidNumber;
    private int amountOfSameModules = 0;
    private int currentTypingIndex = 0;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        //NEEDED, DONT TOUCH
        moduleId = moduleIdCounter++;

        //Gives each selectable object a function
        foreach (KMSelectable button in buttons)
        {
            button.OnInteract += delegate () { PressButton(button); return false; };
        }
    }

    void Start()
    {
        float scalar = this.transform.lossyScale.x;
        for (var i = 0; i < allLights.Length; i++)
        {
            allLights[i].range *= scalar;
        }

        timerRef = StartCoroutine(NewNumberAndTimer(true));
    }

    

    private void RestartTimer()
    {
        timerText.text = "---";
        StopCoroutine(timerRef);
        timerRef = StartCoroutine(NewNumberAndTimer());
    }

    IEnumerator NewNumberAndTimer(bool delay = false)
    {
        if (stage == 2)
        {
            yield break;
        }

        lightColours[0] = colourStrings[6];
        ledArray[0].GetComponent<MeshRenderer>().material = lightMaterials[6];
        ledArray[0].GetComponentInChildren<Light>().color = Color.black;
        colourblindText[0].GetComponent<TextMesh>().text = string.Empty;
        lightColours[1] = colourStrings[6];
        ledArray[1].GetComponent<MeshRenderer>().material = lightMaterials[6];
        ledArray[1].GetComponentInChildren<Light>().color = Color.black;
        colourblindText[1].GetComponent<TextMesh>().text = string.Empty;

        for (int i = 0; i < 4; i++)
        {
            displayText.text = displayText.text.Remove(i, 1).Insert(i, "-");

            yield return new WaitForSecondsRealtime(0.25f);
        }

        //Advances stage if requirement is met
        if (acceptedNumbers.Count >= 1 && (acceptedNumbers.Count + rejectedNumbers.Count >= 12))
        {
            Debug.LogFormat("[Remember Me Now #{0}] The amount of accepted numbers plus the amount of rejected numbers is greater than or equal to 12 and there's at least 1 accepted number, switching to stage 2.", moduleId);
            stage = 2;
            timerText.text = acceptedNumbers.Count.ToString();
            if (timerText.text.Length == 2)
            {
                timerText.text = "0" + timerText.text;
            }
            else if (timerText.text.Length == 1)
            {
                timerText.text = "00" + timerText.text;
            }

            //Initial log
            if (rejectedNumbers.Count > 0)
            {
                Debug.LogFormat("[Remember Me Now #{0}] ABS({1} - {2}) = {3}. The current number to submit is {3}.", moduleId, acceptedNumbers[0], rejectedNumbers[0], Mathf.Abs(acceptedNumbers[0] - rejectedNumbers[0]));
            }
            else
            {
                Debug.LogFormat("[Remember Me Now #{0}] The current number to submit is {1}.", moduleId, acceptedNumbers[0]);
            }
            yield break;
        }

        for (int i = 0; i < 4; i++)
        {
            displayText.text = displayText.text.Remove(i, 1).Insert(i, UnityEngine.Random.Range(0, 10).ToString());

            yield return new WaitForSecondsRealtime(0.25f);
        }

        //LED handler
        int temp1 = UnityEngine.Random.Range(0, 6);
        lightColours[0] = colourStrings[temp1];
        ledArray[0].GetComponent<MeshRenderer>().material = lightMaterials[temp1];
        ledArray[0].GetComponentInChildren<Light>().color = colours[temp1];
        colourblindText[0].GetComponent<TextMesh>().text = lightColours[0][0].ToString();
        if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
        {
            int temp2 = UnityEngine.Random.Range(0, 6);
            while (temp2 == temp1)
            {
                temp2 = UnityEngine.Random.Range(0, 6);
            }

            lightColours[1] = colourStrings[temp2];
            ledArray[1].GetComponent<MeshRenderer>().material = lightMaterials[temp2];
            ledArray[1].GetComponentInChildren<Light>().color = colours[temp2];
            colourblindText[1].GetComponent<TextMesh>().text = lightColours[1][0].ToString();
        }

        Debug.LogFormat("[Remember Me Now #{0}] The current number is {1}, the colour of the LED(s) are/is {2}{3}{4}.", moduleId, displayText.text, lightColours[0], lightColours[1] != "Off" ? " and " : string.Empty, lightColours[1] != "Off" ? lightColours[1] : string.Empty);

        //validity handler
        switch (lightColours[0])
        {
            case "Off":
                isValidNumber = true;
                break;
            case "Red":
                if (rejectedNumbers.Count > 0)
                {
                    isValidNumber = rejectedNumbers[rejectedNumbers.Count - 1] > int.Parse(displayText.text);
                }
                else
                {
                    isValidNumber = false;
                }
                break;
            case "Blue":
                isValidNumber = int.Parse(displayText.text) > 5000;
                break;
            case "Yellow":
                isValidNumber = acceptedNumbers.Count < rejectedNumbers.Count;
                break;
            case "Green":
                isValidNumber = displayText.text[0] != '0';
                break;
            case "Orange":
                isValidNumber = acceptedNumbers.Count % 2 == 0;
                break;
            case "Purple":
                isValidNumber = lightColours[1] != "Off";
                break;
        }

        Debug.LogFormat("[Remember Me Now #{0}] The {1} statement is {2}.", moduleId, lightColours[0], isValidNumber ? "true" : "false");

        if (lightColours[1] != "Off")
        {
            bool tempBool = false;
            switch (lightColours[1])
            {
                case "Red":
                    if (rejectedNumbers.Count > 0)
                    {
                        tempBool = rejectedNumbers[rejectedNumbers.Count - 1] > int.Parse(displayText.text);
                    }
                    else
                    {
                        tempBool = false;
                    }
                    break;
                case "Blue":
                    tempBool = int.Parse(displayText.text) > 5000;
                    break;
                case "Yellow":
                    tempBool = acceptedNumbers.Count < rejectedNumbers.Count;
                    break;
                case "Green":
                    tempBool = displayText.text[0] != '0';
                    break;
                case "Orange":
                    tempBool = acceptedNumbers.Count % 2 == 0;
                    break;
                case "Purple":
                    tempBool = lightColours[1] != "Off";
                    break;
            }

            Debug.LogFormat("[Remember Me Now #{0}] The {1} statement is {2}.", moduleId, lightColours[1], tempBool ? "true" : "false");

            if (isValidNumber && tempBool)
            {
                Debug.LogFormat("[Remember Me Now #{0}] Both statements are true, therefore the current number is not valid", moduleId);
            }
            else if (isValidNumber ^ tempBool)
            {
                Debug.LogFormat("[Remember Me Now #{0}] Only one statement is true, therefore the current number is valid", moduleId);
            }
            else
            {
                Debug.LogFormat("[Remember Me Now #{0}] Neither statements are true, therefore the current number is not valid", moduleId);
            }

            isValidNumber = isValidNumber ^ tempBool;
        }

        if (lightColours[1] == "Off")
        {
            Debug.LogFormat("[Remember Me Now #{0}] The current number is {1}", moduleId, isValidNumber ? "valid" : "not valid");
        }
        
        //Logs the amount of RMN modules are on the bomb
        if (amountOfSameModules == 0)
        {
            for (int i = 0; i < bombRef.GetSolvableModuleNames().Count; i++)
            {
                if (bombRef.GetModuleNames()[i] == "Remember Me Now")
                {
                    amountOfSameModules++;
                }
            }
        }

        Debug.LogFormat("[Remember Me Now #{0}] There is {1} instance(s) of Remember Me Now. Timer set to {2}", moduleId, amountOfSameModules, amountOfSameModules * 40 + (delay ? 45 : 0));
        timerText.text = (amountOfSameModules * 40 + (delay ? 45 : 0)).ToString();

        if (timerText.text.Length == 2)
        {
            timerText.text = "0" + timerText.text;
        }
        else if (timerText.text.Length == 1)
        {
            timerText.text = "00" + timerText.text;
        }

        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);

            timerText.text = (int.Parse(timerText.text) - 1).ToString();

            if (timerText.text.Length == 2)
            {
                timerText.text = "0" + timerText.text;
            }
            else if (timerText.text.Length == 1)
            {
                timerText.text = "00" + timerText.text;
            }

            if (timerText.text == "000")
            {
                bombModuleRef.HandleStrike();

                Debug.LogFormat("[Remember Me Now #{0}] The timer has ran out, clearing all remembered accepted and rejected numbers.", moduleId);

                acceptedNumbers.Clear();
                rejectedNumbers.Clear();
                RestartTimer();
            }
        }
    }

    //When a button is pushed
    void PressButton(KMSelectable pressedButton)
    {
        if (moduleSolved)
        {
            return;
        }

        if (pressedButton.name[0] == 'L')
        {
            audioRef.PlaySoundAtTransform("SmallKeyPress", pressedButton.transform);
            pressedButton.AddInteractionPunch(0.5f);
            colourblindText[0].SetActive(!colourblindText[0].activeSelf);
            colourblindText[1].SetActive(!colourblindText[1].activeSelf);
        }

        if (pressedButton.name == "Accept_Button")
        {
            audioRef.PlaySoundAtTransform("BigKey", pressedButton.transform);
            pressedButton.AddInteractionPunch(1f);

            if (stage == 1 && timerText.text != "---")
            {
                if (!isValidNumber)
                {
                    bombModuleRef.HandleStrike();
                    Debug.LogFormat("[Remember Me Now #{0}] \"Accept\" was pressed incorrectly, {1} is being remembered as an accepted number.", moduleId, displayText.text);
                }
                else
                {
                    Debug.LogFormat("[Remember Me Now #{0}] \"Accept\" was pressed correctly, {1} is being remembered as an accepted number.", moduleId, displayText.text);
                }
                acceptedNumbers.Add(int.Parse(displayText.text));
                RestartTimer();
                pressedButton.AddInteractionPunch(1f);
            }
            else if (stage == 2)
            {
                int throwawayInt;
                if (!int.TryParse(displayText.text, out throwawayInt))
                {
                    return;
                }

                if (rejectedNumbers.Count > 0)
                {
                    if (int.Parse(displayText.text) == Mathf.Abs(acceptedNumbers[0] - rejectedNumbers[0]))
                    {
                        acceptedNumbers.RemoveAt(0);
                        rejectedNumbers.RemoveAt(0);

                        Debug.LogFormat("[Remember Me Now #{0}] {1} submitted correctly, {2} more numbers to submit.", moduleId, displayText.text, acceptedNumbers.Count);

                        displayText.text = "----";
                        currentTypingIndex = 0;
                        timerText.text = acceptedNumbers.Count.ToString();

                        if (timerText.text.Length == 2)
                        {
                            timerText.text = "0" + timerText.text;
                        }
                        else if (timerText.text.Length == 1)
                        {
                            timerText.text = "00" + timerText.text;
                        }

                        //Logging next number
                        if (acceptedNumbers.Count != 0)
                        {
                            if (rejectedNumbers.Count > 0)
                            {
                                Debug.LogFormat("[Remember Me Now #{0}] ABS({1} - {2}) = {3}. The current number to submit is {3}.", moduleId, acceptedNumbers[0], rejectedNumbers[0], Mathf.Abs(acceptedNumbers[0] - rejectedNumbers[0]));
                            }
                            else
                            {
                                Debug.LogFormat("[Remember Me Now #{0}] The current number to submit is {1}.", moduleId, acceptedNumbers[0]);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogFormat("[Remember Me Now #{0}] {1} submitted incorrectly, incurring strike.", moduleId, displayText.text);

                        displayText.text = "----";
                        currentTypingIndex = 0;

                        bombModuleRef.HandleStrike();
                    }
                }
                else
                {
                    if (int.Parse(displayText.text) == acceptedNumbers[0])
                    {
                        acceptedNumbers.RemoveAt(0);

                        Debug.LogFormat("[Remember Me Now #{0}] {1} submitted correctly, {2} more numbers to submit.", moduleId, displayText.text, acceptedNumbers.Count);

                        displayText.text = "----";
                        currentTypingIndex = 0;
                        timerText.text = acceptedNumbers.Count.ToString();

                        if (timerText.text.Length == 2)
                        {
                            timerText.text = "0" + timerText.text;
                        }
                        else if (timerText.text.Length == 1)
                        {
                            timerText.text = "00" + timerText.text;
                        }

                        //Logging next number
                        if (acceptedNumbers.Count != 0)
                        {
                            Debug.LogFormat("[Remember Me Now #{0}] The current number to submit is {1}.", moduleId, acceptedNumbers[0]);
                        }
                    }
                    else
                    {
                        Debug.LogFormat("[Remember Me Now #{0}] {1} submitted incorrectly, incurring strike.", moduleId, displayText.text);

                        displayText.text = "----";
                        currentTypingIndex = 0;

                        bombModuleRef.HandleStrike();
                    }
                }

                if (acceptedNumbers.Count == 0)
                {
                    Debug.LogFormat("[Remember Me Now #{0}] No more numbers to submit, module solved.", moduleId);
                    moduleSolved = true;
                    StartCoroutine(PassAnimation());
                    bombModuleRef.HandlePass();
                }
            }
        }
        else if (pressedButton.name == "Reject_Button")
        {
            audioRef.PlaySoundAtTransform("BigKey", pressedButton.transform);
            pressedButton.AddInteractionPunch(1f);

            if (stage == 1 && timerText.text != "---")
            {
                if (isValidNumber)
                {
                    bombModuleRef.HandleStrike();
                    Debug.LogFormat("[Remember Me Now #{0}] \"Reject\" was pressed incorrectly, {1} is being remembered as an rejected number.", moduleId, displayText.text);
                }
                else
                {
                    Debug.LogFormat("[Remember Me Now #{0}] \"Reject\" was pressed correctly, {1} is being remembered as an rejected number.", moduleId, displayText.text);
                }
                rejectedNumbers.Add(int.Parse(displayText.text));
                RestartTimer();
                pressedButton.AddInteractionPunch(1f);
            }
            else if (stage == 2)
            {
                displayText.text = "----";
                currentTypingIndex = 0;
            }
        }

        if (stage == 2 && pressedButton.name.Substring(0, 3) == "Key" && currentTypingIndex < 4)
        {
            audioRef.PlaySoundAtTransform("KeyPress", pressedButton.transform);
            pressedButton.AddInteractionPunch(0.5f);
            displayText.text = displayText.text.Remove(currentTypingIndex, 1).Insert(currentTypingIndex, pressedButton.name.Substring(3));
            currentTypingIndex++;
        }
    }

    private IEnumerator PassAnimation()
    {
        audioRef.PlaySoundAtTransform("PassSound", this.transform);
        while (true)
        {
            displayText.text = "Good";

            yield return new WaitForSecondsRealtime(1f);

            displayText.text = "Job";

            yield return new WaitForSecondsRealtime(1f);
        }
    }

    //Twitch Plays support

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} accept/a [Presses the Accept button] | !{0} 0378 [Inputs 0378 using the keypad. Can only be done in stage 2] | !{0} reject/r [Presses the Reject button] | !{0} colourblind/colorblind/cb [Turns on/off colourblind mode]";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (command.EqualsIgnoreCase("accept") || command.EqualsIgnoreCase("a"))
        {
            int throwawayInt;
            if (!int.TryParse(displayText.text, out throwawayInt))
            {
                yield return "sendtochaterror The submitted number does not have leading zeros!";
                yield break;
            }
            yield return null;
            buttons[10].OnInteract();
            yield break;
        }

        if (command.EqualsIgnoreCase("reject") || command.EqualsIgnoreCase("r"))
        {
            yield return null;
            buttons[11].OnInteract();
            yield break;
        }

        if (command.EqualsIgnoreCase("colourblind") || command.EqualsIgnoreCase("colorblind") ||command.EqualsIgnoreCase("cb"))
        {
            yield return null;
            buttons[12].OnInteract();
            yield break;
        }

        int tempInt = -1;
        if (stage == 1)
        {
            yield return "sendtochaterror The specified input is invalid!";
            yield break;
        }
        else if (!int.TryParse(command, out tempInt))
        {
            yield return "sendtochaterror The specified input is invalid!";
            yield break;
        }
        else if (tempInt < 0 || tempInt > 9999)
        {
            yield return "sendtochaterror The specified input is invalid! Numbers have to be between 0 & 9999!";
            yield break;
        }
        else
        {
            yield return null;
            for (int i = 0; i < command.Length; i ++)
            {
                if (command[i] == '0')
                    buttons[9].OnInteract();
                else
                    buttons[int.Parse(command[i].ToString()) - 1].OnInteract();
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        //While in stage 1 accept valid numbers, reject invalid numbers
        while (stage == 1)
        {
            yield return null;

            //If statement waits for the module to be ready for input before attempting to press a button
            if (timerText.text != "---")
            {
                yield return new WaitForSecondsRealtime(0.1f);

                buttons[isValidNumber ? 10 : 11].OnInteract();
            }
        }

        int numberToSubmit;
        string tempString;

        while (stage == 2 && acceptedNumbers.Count > 0)
        {
            yield return null;

            //Empties the display in case there was already input
            if (displayText.text != "----")
            {
                buttons[11].OnInteract();
            }

            //If there's still accepted numbers left, continue
            if (acceptedNumbers.Count > 0)
            {
                //Calculates the number to submit based on if there's rejected numbers left
                if (rejectedNumbers.Count > 0)
                {
                    numberToSubmit = Mathf.Abs(acceptedNumbers[0] - rejectedNumbers[0]);
                }
                else
                {
                    numberToSubmit = acceptedNumbers[0];
                }

                //Adds leading zeros if needed
                tempString = numberToSubmit.ToString("0000");

                //Enters the number one digit at a time
                for (int i = 0; i < 4; i++)
                {
                    yield return new WaitForSecondsRealtime(0.1f);

                    if (tempString[i] != '0')
                    {
                        buttons[int.Parse(tempString[i].ToString()) - 1].OnInteract();
                    }
                    else
                    {
                        buttons[9].OnInteract();
                    }
                }

                yield return new WaitForSecondsRealtime(0.1f);

                buttons[10].OnInteract();
            }
        }
    }
}
