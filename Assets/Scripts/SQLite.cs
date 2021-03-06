﻿using GeneticAlgorithm;
using Mono.Data.Sqlite;
using System.Data;
using UnityEngine;
using System.IO;
//used to load the data from SQL DataBase
public class SQLite : MonoBehaviour
{
    private static IDbConnection dbcon;
    static string connection;

    static void OpenDB(string p)
    {
#if (UNITY_ANDROID && !UNITY_EDITOR)
        string filepath = Application.persistentDataPath + "/" + p;
		if (!File.Exists(filepath))// if it doesn't ->
		{
			WWW loadDB = new WWW("jar:file://" + Application.dataPath + "!/assets/" + p);  // this is the path to your StreamingAssets in android
			while (!loadDB.isDone) { }  // CAREFUL here, for safety reasons you shouldn't let this while loop unattended, place a timer and error check
			File.WriteAllBytes(filepath, loadDB.bytes);// then save to Application.persistentDataPath
		}
#elif 	(UNITY_EDITOR)
        string filepath = string.Format(@"Assets/StreamingAssets/{0}", p);
#else
		string filepath = Application.persistentDataPath + "/" + p;
		string loadDb = Application.dataPath + "/StreamingAssets/" + p;
		if (!File.Exists(filepath))
		{
		File.Copy(loadDb, filepath);
		}
#endif
        connection = "URI=file:" + filepath;
        dbcon = new SqliteConnection(connection);
        dbcon.Open();
    }

    public void LoadDataFromDB()
    {
        try
        {
            OpenDB("placement.bytes");
            SendCmd("PRAGMA foreign_keys = ON");

            LoadData(DataStorage.CurrentProject, DataStorage.CurrentPlate);

            View.DebugText("Data was loaded successfully. " + System.Environment.NewLine);
            View.DebugAppendText("Project "+ "\"" + DataStorage.ProjectNames[DataStorage.CurrentProject - 1] 
                + "\", " + "plate " + DataStorage.CurrentPlate + System.Environment.NewLine);
            View.DebugAppendText("Number of elements: " + DataStorage.cm.Length + System.Environment.NewLine);
            View.DebugAppendText("PCB width: " + PP.Width);
        }
        catch (System.Exception e)
        {
            View.DebugText("Error " + e.Message);
        }
        finally
        {
            dbcon.Close();
            dbcon = null;
        }
    }

    public static void LoadProjectNames()
    {
        try
        {
            OpenDB("placement.bytes");
            SendCmd("PRAGMA foreign_keys = ON");

            IDbCommand dbcmd = dbcon.CreateCommand();

            string sqlQuery = "SELECT Project.Project_Name FROM Project;";
            dbcmd.CommandText = sqlQuery;
            IDataReader reader = dbcmd.ExecuteReader();
            while (reader.Read())
            {
                DataStorage.ProjectNames.Add(reader.GetString(0));
            }
            reader.Close();

        }
        catch (System.Exception e)
        {
            View.DebugText("Error " + e.Message);
        }
        finally
        {
            dbcon.Close();
            dbcon = null;
        }
    }

    public static void LoadPlates()
    {
        try
        {
            OpenDB("placement.bytes");
            SendCmd("PRAGMA foreign_keys = ON");

            for (int i = 0; i < DataStorage.ProjectNames.Count; i++)
            {
                IDbCommand dbcmd = dbcon.CreateCommand();
                string sqlQuery = "SELECT Plate.Number FROM Plate, Project WHERE (Project.Project_Name = \"" + DataStorage.ProjectNames[i] + "\") & (Plate.Project_Number = Project.Project_Number);";
                dbcmd.CommandText = sqlQuery;
                IDataReader reader = dbcmd.ExecuteReader();
                DataStorage.PlateNumbers.Add(new System.Collections.Generic.List<int>());
                while (reader.Read())
                {
                    DataStorage.PlateNumbers[i].Add(reader.GetInt32(0));
                }
                reader.Close();
            }
        }
        catch (System.Exception e)
        {
            View.DebugText("Error " + e.Message);
        }
        finally
        {
            dbcon.Close();
            dbcon = null;
        }
    }

    static void LoadData(int numProject, int numPlate)
    {
        IDbCommand dbcmd = dbcon.CreateCommand();

        string sqlQuery = "SELECT CircuitElement.CircuitElementName, Elements.Width, Elements.Height, Elements.Model, Elements.Name, Elements.PowerDissipation FROM CircuitElement, Elements WHERE (Elements.Name = CircuitElement.ElementName) & (CircuitElement.Project_Number = " + numProject + ");";
        dbcmd.CommandText = sqlQuery;
        IDataReader reader = dbcmd.ExecuteReader();

        System.Collections.Generic.List<CircuitElement> elements = new System.Collections.Generic.List<CircuitElement>();
		DataStorage.caseNames = new System.Collections.Generic.List<string>();
        while (reader.Read())
        {
            string caseName = reader.GetString(3);
            elements.Add(new CircuitElement(reader.GetString(0),
                reader.GetInt32(1), reader.GetInt32(2), caseName, reader.GetString(4), reader.GetFloat(5)));
            if (!DataStorage.caseNames.Contains(caseName))
                DataStorage.caseNames.Add(caseName);
        }
        reader.Close();

        DataStorage.cm = elements.ToArray(); ;
        elements.Clear();

        int n = DataStorage.cm.Length; DataStorage.N = n; DataStorage.C = new int[n, n];

        sqlQuery = "SELECT Plate.Width, Plate.Height FROM Plate WHERE (Plate.Number = " + numProject + ");"; 
        dbcmd.CommandText = sqlQuery;
        reader = dbcmd.ExecuteReader();
        while (reader.Read())
        {
            PP.Width = reader.GetInt32(0);
            PP.Height = reader.GetInt32(1);
        }
        reader.Close();
        PP.Plate = new int[PP.Width, PP.Height];

        sqlQuery = "SELECT Chain.Element1, Chain.Element2 FROM Chain WHERE (Chain.Project_Number = " + numProject + ");";
        dbcmd.CommandText = sqlQuery;
        reader = dbcmd.ExecuteReader();

        while (reader.Read())
        {
            DataStorage.C[reader.GetInt32(0) - 1, reader.GetInt32(1) - 1]++;
            DataStorage.C[reader.GetInt32(1) - 1, reader.GetInt32(0) - 1]++;
        }
        reader.Close();

        reader.Close();
        reader = null;
        dbcmd.Dispose();
        dbcmd = null;
    }

    static void SendCmd(string cmd)
    {
        IDbCommand dbcmd = dbcon.CreateCommand();
        string sqlQuery = cmd;
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();
        dbcmd.Dispose();
        dbcmd = null;
    }
}