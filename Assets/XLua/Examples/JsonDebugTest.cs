using UnityEngine;
using XLua;

public class JsonDebugTest : MonoBehaviour
{
    void Start()
    {
        LuaEnv luaEnv = new LuaEnv();

        try
        {
            // Test: Create LuaTable from JSON
            string json = "{\"a\":1, \"b\":\"hello\"}";
            Debug.Log("Input JSON: " + json);
            
            LuaTable table = new LuaTable(json, luaEnv);
            
            // Check if values are accessible
            Debug.Log("Checking table contents:");
            int a = table.Get<int>("a");
            string b = table.Get<string>("b");
            Debug.Log("table['a'] = " + a);
            Debug.Log("table['b'] = " + b);
            
            // Try to iterate through the table
            Debug.Log("Iterating through table:");
            table.ForEach<string, object>((key, value) => {
                Debug.Log($"  {key} = {value}");
            });
            
            // Try ToJson
            Debug.Log("Calling ToJson...");
            string jsonOut = table.ToJson();
            Debug.Log("Output JSON: '" + jsonOut + "'");
            Debug.Log("Output JSON length: " + jsonOut.Length);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception: " + e.Message);
            Debug.LogError(e.StackTrace);
        }
        finally
        {
            luaEnv.Dispose();
        }
    }
}
