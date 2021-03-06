﻿using GeneticAlgorithm;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class View : MonoBehaviour
{

    public Transform Plate_prefab, Rotator;

    [SerializeField]
    InputField PCross, PMut, numPerson;

    public Dropdown project_num, plate_num;

    public Slider criterion;

    static Text DebugTxt;

    [SerializeField]
    GameObject SettingsMenu, Footer, ProjectMenu;

    [SerializeField]
    Button SaveButton, TButton;
    bool T = false;

    void Start()
    {
        DataStorage.CurrentProject = 1;
        DataStorage.CurrentPlate = 1;
        SettingsMenu.SetActive(false);
        Footer.SetActive(false);
        SaveButton.interactable = false;
        TButton.interactable = false;
        DebugTxt = GameObject.Find("Log_label").GetComponent("Text") as Text;
        project_num.ClearOptions();
        plate_num.ClearOptions();

        SQLite.LoadProjectNames(); SQLite.LoadPlates();

        Messenger.AddListener(GameEvent.GUI_UPDATED, ShowElements);

        foreach (string project in DataStorage.ProjectNames)
            project_num.options.Add(new Dropdown.OptionData() { text = (string)project });
        if (project_num.options.Count > 0)
        {
            project_num.value = -1;
            project_num.value = 0;
        }
    }

    void OnDestroy()
    {
        Messenger.RemoveListener(GameEvent.GUI_UPDATED, ShowElements);
    }

    static public void DebugText(string s)
    {
        if (DebugTxt != null)
            DebugTxt.text = s;
    }

    static public void DebugAppendText(string s)
    {
        if (DebugTxt != null)
            DebugTxt.text += s;
    }

    private void ShowElements()
    {
        SendDestroyMessage();

        Transform Plate = Instantiate(Plate_prefab) as Transform;
        NewPlate(Plate);

        Transform Elem;
        List<GameObject> lst = new List<GameObject>();
        foreach (string caseName in DataStorage.caseNames)
            lst.Add(Resources.Load("Elements/" + caseName) as GameObject);

        for (int i = 0; i < DataStorage.N; i++)
        {
            Elem = Instantiate(lst[DataStorage.caseNames.IndexOf(DataStorage.cm[i].CaseName)].transform) as Transform;

            Elem.parent = Rotator;
            Elem.localPosition = new Vector3(DataStorage.cm[i].x + DataStorage.cm[i].Width / 2, 
                DataStorage.cm[i].y + DataStorage.cm[i].Height / 2, 0); // make it at the position of the spawner

            Elem.localEulerAngles = Vector3.zero; //default angle
            if (DataStorage.cm[i].isVertical == true)
            {
                Elem.localEulerAngles = new Vector3(Elem.rotation.x, Elem.rotation.y, Elem.rotation.z + 90);
            }

            Elem.gameObject.name = i.ToString();
            Elem.Find("Label").GetComponent<TextMesh>().text = DataStorage.cm[i].Name;

            T = false;
        }
    }

    public void Assembling_Draft()
    {
        SendDestroyMessage();
        GameObject elem_prefab = Resources.Load("Elements/DraftElement") as GameObject;

        Transform Plate = Instantiate((Resources.Load("Elements/DraftPlate") as GameObject).transform,
            new Vector3(PP.Width / 2, -0.5f, PP.Height / 2), Quaternion.identity) as Transform;

        NewPlate(Plate);

        Transform Elem;
        for (int i = 0; i < DataStorage.N; i++)
        {
            CircuitElement t = DataStorage.cm[i];
            Elem = Instantiate(elem_prefab.transform) as Transform;

            Elem.Find("Scale").transform.localScale = new Vector3(t.Width - 3, t.Height - 3, 1);

            Elem.parent = Rotator;

            Elem.localPosition = new Vector3(t.x + t.Width / 2, t.y + t.Height / 2, 0);

            Elem.localEulerAngles = Vector3.zero;

            Elem.gameObject.name = i.ToString();
            Elem.Find("Label").GetComponent<TextMesh>().text = DataStorage.cm[i].Name;
        }

        HiResScreenShots.TakeHiResShot(new Vector3(PP.Width / 2, PP.Height / 2));
        SaveButton.interactable = false;
    }

    private static void SendDestroyMessage()
    {
        try
        {
            Messenger.Broadcast(GameEvent.DESTROY);
        }
        catch
        { }
    }

    public Transform elemT;
    public void ViewTM() //Thermal map button
    {
        if (!T) //if thermal map is not showing
        {
            SendDestroyMessage();
            Transform Plate = Instantiate(Plate_prefab) as Transform;
            NewPlate(Plate);
            Transform Elem;
            for (int i = 0; i < DataStorage.N; i++)
            {
                if (DataStorage.cm[i].pwDissipation > 1)
                {
                    Elem = Instantiate(elemT) as Transform;
                    Elem.parent = Rotator;
                    Elem.localPosition = new Vector3(DataStorage.cm[i].x + DataStorage.cm[i].Width / 2,
                        DataStorage.cm[i].y + DataStorage.cm[i].Height / 2, -1); // make it at the exact position of the spawner
                    Elem.localEulerAngles = Vector3.zero;
                    float p = (DataStorage.cm[i].pwDissipation * 5); //power dissipation value
                    Elem.localScale = new Vector3(p, p, p);
                    Elem.gameObject.name = i.ToString(); //set new name to game object
                }
            }
            T = true;
            DebugText("Power dissipation");
        }
        else
        {
            ShowElements();
            DebugText("Elements");
        }
    }

    private void NewPlate(Transform Plate)
    {
        Plate.parent = Rotator;
        Plate.localEulerAngles = Vector3.zero;
        Plate.localScale = new Vector3(PP.Width, PP.Height, 1);
    }

    public void ProjectChanged()
    {
        DataStorage.CurrentProject = project_num.value + 1;
        DataStorage.CurrentPlate = 1;

        plate_num.ClearOptions();

        foreach (int plates in DataStorage.PlateNumbers[DataStorage.CurrentProject - 1])
            plate_num.options.Add(new Dropdown.OptionData() { text = plates.ToString() }); 

        if (plate_num.options.Count > 0)
        {
            plate_num.value = -1;
            plate_num.value = 0;
        }
    }

    public void PlateChanged()
    {
        DataStorage.CurrentPlate = plate_num.value + 1;
    }

    public void CriterionChanged()
    {
        DataStorage.p2 = DataStorage.p2_start * criterion.value;
        DataStorage.p1 = (1 - criterion.value) * DataStorage.p1_start;
        DebugText("Criterion 1: " + (int)(criterion.value * 100) + "%" + System.Environment.NewLine);
        DebugAppendText("Criterion 2: " + (int)((1 - criterion.value) * 100) + "%" + System.Environment.NewLine);
    }

    public void numPersonChanged() //changes number of persons in the GA
    {
        if (int.Parse(numPerson.text) < 2)
            numPerson.text = 2 + "";
        DataStorage.NPer = int.Parse(numPerson.text);
    }

    public void PCrossChanged()
    {
        double t = double.Parse(PCross.text);

        if (t < 0)
            PCross.text = 0 + "";
        else if (t > 1)
            PCross.text = 1 + "";

        DataStorage.PCross = double.Parse(PCross.text);
    }

    public void PMutChanged()
    {
        double t = double.Parse(PMut.text);

        if (t < 0)
            PMut.text = 0 + "";
        else if (t > 1)
            PMut.text = 1 + "";

        DataStorage.PMut = int.Parse(PMut.text);
    }

    public void ShowHideSettingsMenu()
    {
        SettingsMenu.SetActive(!SettingsMenu.activeSelf);
    }

    public void showHideMenu()
    {
        ProjectMenu.SetActive(!ProjectMenu.activeSelf);
        Footer.SetActive(!Footer.activeSelf);
    }

    public void PerformMainCoroutine()
    {
        DebugText("Placement in progress...");
        StartCoroutine(this.gameObject.GetComponent<Main>().MainCoroutine());
        SaveButton.interactable = true;
        TButton.interactable = true;
    }
}