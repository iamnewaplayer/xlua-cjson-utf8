using UnityEngine;
using XLua;
using System.Collections.Generic;

public class JsonTest : MonoBehaviour
{
    void Start()
    {
        LuaEnv luaEnv = new LuaEnv();

        Debug.Log("Starting LuaTable JSON Test...");

        try
        {
            // Test 1: Create LuaTable from JSON
            string json = "{\"a\":1, \"b\":\"hello\", \"c\":[1,2,3]}";
            Debug.Log("Input JSON: " + json);
            
            LuaTable table = new LuaTable(json, luaEnv);
            
            int a = table.Get<int>("a");
            string b = table.Get<string>("b");
            
            Debug.Log("table['a'] = " + a);
            Debug.Log("table['b'] = " + b);
            
            if (a != 1 || b != "hello")
            {
                Debug.LogError("Test 1 Failed: Incorrect values in table");
            }
            else
            {
                Debug.Log("Test 1 Passed");
            }

            // Test 2: Convert LuaTable to JSON
            string jsonOut = table.ToJson();
            Debug.Log("Output JSON: " + jsonOut);
            
            // Verify round trip (simple check)
            if (jsonOut.Contains("\"a\":1") && jsonOut.Contains("\"b\":\"hello\""))
            {
                Debug.Log("Test 2 Passed");
            }
            else
            {
                Debug.LogError("Test 2 Failed: Output JSON missing expected fields");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Test Exception: " + e.Message);
            Debug.LogError(e.StackTrace);
        }
        finally
        {
            luaEnv.Dispose();
        }
    }
}
