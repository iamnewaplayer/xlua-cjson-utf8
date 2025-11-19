/*
 * Tencent is pleased to support the open source community by making xLua available.
 * Copyright (C) 2016 THL A29 Limited, a Tencent company. All rights reserved.
 * Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
 * http://opensource.org/licenses/MIT
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
*/

#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace XLua
{
    public partial class LuaTable : LuaBase
    {
        public LuaTable(int reference, LuaEnv luaenv) : base(reference, luaenv)
        {
        }

        /// <summary>
        /// Create a LuaTable from a JSON string.
        /// This uses C-side cjson for efficient parsing without C# overhead.
        /// Note: This does NOT affect the standard Lua cjson module or any existing Lua code.
        /// </summary>
        /// <param name="json">JSON string to parse (e.g., "{\"a\":1, \"b\":\"hello\"}")</param>
        /// <param name="luaenv">LuaEnv instance to create the table in</param>
        /// <exception cref="Exception">Thrown if JSON parsing fails</exception>
        /// <example>
        /// var table = new LuaTable("{\"a\":1, \"b\":\"hello\"}", luaEnv);
        /// int a = table.Get&lt;int&gt;("a");  // Returns 1
        /// </example>
        public LuaTable(string json, LuaEnv luaenv) : base(CreateTableFromJson(json, luaenv), luaenv)
        {
        }

        /// <summary>
        /// Helper method to create a Lua table reference from JSON string.
        /// Called before base constructor to properly initialize the readonly luaReference field.
        /// </summary>
        private static int CreateTableFromJson(string json, LuaEnv luaenv)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaenv.luaEnvLock)
            {
#endif
                var L = luaenv.L;
                int oldTop = LuaAPI.lua_gettop(L);
                try
                {
                    // Push JSON string and decode it to a Lua table
                    LuaAPI.lua_pushstring(L, json);
                    if (0 != LuaAPI.xlua_cjson_decode(L))
                    {
                        // Error - pcall returns non-zero on error
                        string err = LuaAPI.lua_tostring(L, -1);
                        LuaAPI.lua_settop(L, oldTop);
                        throw new Exception("cjson decode error: " + err);
                    }
                    // Success, table is at top - create a reference to it
                    int refId = LuaAPI.luaL_ref(L);
                    LuaAPI.lua_settop(L, oldTop);
                    return refId;
                }
                catch
                {
                    LuaAPI.lua_settop(L, oldTop);
                    throw;
                }
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        // no boxing version get
        public void Get<TKey, TValue>(TKey key, out TValue value)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
                var L = luaEnv.L;
                var translator = luaEnv.translator;
                int oldTop = LuaAPI.lua_gettop(L);
                LuaAPI.lua_getref(L, luaReference);
                translator.PushByType(L, key);

                if (0 != LuaAPI.xlua_pgettable(L, -2))
                {
                    string err = LuaAPI.lua_tostring(L, -1);
                    LuaAPI.lua_settop(L, oldTop);
                    throw new Exception("get field [" + key + "] error:" + err);
                }

                LuaTypes lua_type = LuaAPI.lua_type(L, -1);
                Type type_of_value = typeof(TValue);
                if (lua_type == LuaTypes.LUA_TNIL && type_of_value.IsValueType())
                {
                    throw new InvalidCastException("can not assign nil to " + type_of_value.GetFriendlyName());
                }

                try
                {
                    translator.Get(L, -1, out value);
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    LuaAPI.lua_settop(L, oldTop);
                }
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        // no boxing version get
        public bool ContainsKey<TKey>(TKey key)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
                var L = luaEnv.L;
                var translator = luaEnv.translator;
                int oldTop = LuaAPI.lua_gettop(L);
                LuaAPI.lua_getref(L, luaReference);
                translator.PushByType(L, key);

                if (0 != LuaAPI.xlua_pgettable(L, -2))
                {
                    string err = LuaAPI.lua_tostring(L, -1);
                    LuaAPI.lua_settop(L, oldTop);
                    throw new Exception("get field [" + key + "] error:" + err);
                }

                bool ret =  LuaAPI.lua_type(L, -1) != LuaTypes.LUA_TNIL;

                LuaAPI.lua_settop(L, oldTop);

                return ret;

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        //no boxing version set
        public void Set<TKey, TValue>(TKey key, TValue value)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
                var L = luaEnv.L;
                int oldTop = LuaAPI.lua_gettop(L);
                var translator = luaEnv.translator;

                LuaAPI.lua_getref(L, luaReference);
                translator.PushByType(L, key);
                translator.PushByType(L, value);

                if (0 != LuaAPI.xlua_psettable(L, -3))
                {
                    luaEnv.ThrowExceptionFromError(oldTop);
                }
                LuaAPI.lua_settop(L, oldTop);
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }


        public T GetInPath<T>(string path)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
                var L = luaEnv.L;
                var translator = luaEnv.translator;
                int oldTop = LuaAPI.lua_gettop(L);
                LuaAPI.lua_getref(L, luaReference);
                if (0 != LuaAPI.xlua_pgettable_bypath(L, -1, path))
                {
                    luaEnv.ThrowExceptionFromError(oldTop);
                }
                LuaTypes lua_type = LuaAPI.lua_type(L, -1);
                if (lua_type == LuaTypes.LUA_TNIL && typeof(T).IsValueType())
                {
                    throw new InvalidCastException("can not assign nil to " + typeof(T).GetFriendlyName());
                }

                T value;
                try
                {
                    translator.Get(L, -1, out value);
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    LuaAPI.lua_settop(L, oldTop);
                }
                return value;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void SetInPath<T>(string path, T val)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
                var L = luaEnv.L;
                int oldTop = LuaAPI.lua_gettop(L);
                LuaAPI.lua_getref(L, luaReference);
                luaEnv.translator.PushByType(L, val);
                if (0 != LuaAPI.xlua_psettable_bypath(L, -2, path))
                {
                    luaEnv.ThrowExceptionFromError(oldTop);
                }

                LuaAPI.lua_settop(L, oldTop);
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        [Obsolete("use no boxing version: GetInPath/SetInPath Get/Set instead!")]
        public object this[string field]
        {
            get
            {
                return GetInPath<object>(field);
            }
            set
            {
                SetInPath(field, value);
            }
        }

        [Obsolete("use no boxing version: GetInPath/SetInPath Get/Set instead!")]
        public object this[object field]
        {
            get
            {
                return Get<object>(field);
            }
            set
            {
                Set(field, value);
            }
        }

        public void ForEach<TKey, TValue>(Action<TKey, TValue> action)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
                var L = luaEnv.L;
                var translator = luaEnv.translator;
                int oldTop = LuaAPI.lua_gettop(L);
                try
                {
                    LuaAPI.lua_getref(L, luaReference);
                    LuaAPI.lua_pushnil(L);
                    while (LuaAPI.lua_next(L, -2) != 0)
                    {
                        if (translator.Assignable<TKey>(L, -2))
                        {
                            TKey key;
                            TValue val;
                            translator.Get(L, -2, out key);
                            translator.Get(L, -1, out val);
                            action(key, val);
                        }
                        LuaAPI.lua_pop(L, 1);
                    }
                }
                finally
                {
                    LuaAPI.lua_settop(L, oldTop);
                }
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public int Length
        {
            get
            {
#if THREAD_SAFE || HOTFIX_ENABLE
                lock (luaEnv.luaEnvLock)
                {
#endif
                    var L = luaEnv.L;
                    int oldTop = LuaAPI.lua_gettop(L);
                    LuaAPI.lua_getref(L, luaReference);
                    var len = (int)LuaAPI.xlua_objlen(L, -1);
                    LuaAPI.lua_settop(L, oldTop);
                    return len;
#if THREAD_SAFE || HOTFIX_ENABLE
                }
#endif
            }
        }

#if THREAD_SAFE || HOTFIX_ENABLE
        [Obsolete("not thread safe!", true)]
#endif
        public IEnumerable GetKeys()
        {
            var L = luaEnv.L;
            var translator = luaEnv.translator;
            int oldTop = LuaAPI.lua_gettop(L);
            try
            {
                LuaAPI.lua_getref(L, luaReference);
                LuaAPI.lua_pushnil(L);
                while (LuaAPI.lua_next(L, -2) != 0)
                {
                    yield return translator.GetObject(L, -2);
                    LuaAPI.lua_pop(L, 1);
                }
            }
            finally
            {
                LuaAPI.lua_settop(L, oldTop);
            }
        }

#if THREAD_SAFE || HOTFIX_ENABLE
        [Obsolete("not thread safe!", true)]
#endif
        public IEnumerable<T> GetKeys<T>()
        {
            var L = luaEnv.L;
            var translator = luaEnv.translator;
            int oldTop = LuaAPI.lua_gettop(L);
            try
            {
                LuaAPI.lua_getref(L, luaReference);
                LuaAPI.lua_pushnil(L);
                while (LuaAPI.lua_next(L, -2) != 0)
                {
                    if (translator.Assignable<T>(L, -2))
                    {
                        T v;
                        translator.Get(L, -2, out v);
                        yield return v;
                    }
                    LuaAPI.lua_pop(L, 1);
                }
            }
            finally
            {
                LuaAPI.lua_settop(L, oldTop);
            }
        }

        [Obsolete("use no boxing version: Get<TKey, TValue> !")]
        public T Get<T>(object key)
        {
            T ret;
            Get(key, out ret);
            return ret;
        }

        public TValue Get<TKey, TValue>(TKey key)
        {
            TValue ret;
            Get(key, out ret);
            return ret;
        }

        public TValue Get<TValue>(string key)
        {
            TValue ret;
            Get(key, out ret);
            return ret;
        }

        public void SetMetaTable(LuaTable metaTable)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
                push(luaEnv.L);
                metaTable.push(luaEnv.L);
                LuaAPI.lua_setmetatable(luaEnv.L, -2);
                LuaAPI.lua_pop(luaEnv.L, 1);
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public T Cast<T>()
        {
            var L = luaEnv.L;
            var translator = luaEnv.translator;
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
                push(L);
                T ret = (T)translator.GetObject(L, -1, typeof(T));
                LuaAPI.lua_pop(luaEnv.L, 1);
                return ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        internal override void push(RealStatePtr L)
        {
            LuaAPI.lua_getref(L, luaReference);
        }
        public override string ToString()
        {
            return "table :" + luaReference;
        }

        /// <summary>
        /// Convert this LuaTable to a JSON string.
        /// This uses C-side cjson for efficient encoding without C# overhead.
        /// 
        /// Features:
        /// - Automatically skips unsupported types (functions, userdata, etc.)
        /// - Converts boolean keys to "0" (false) or "1" (true)
        /// - Does NOT affect the standard Lua cjson module or any existing Lua code
        /// 
        /// Note: If the table contains metatables with functions, those functions will be skipped.
        /// </summary>
        /// <returns>JSON string representation of the table</returns>
        /// <exception cref="Exception">Thrown if JSON encoding fails</exception>
        /// <example>
        /// var table = new LuaTable("{\"a\":1, \"b\":\"hello\"}", luaEnv);
        /// string json = table.ToJson();  // Returns: {"a":1,"b":"hello"}
        /// </example>
        public string ToJson()
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
                var L = luaEnv.L;
                int oldTop = LuaAPI.lua_gettop(L);
                
                // Push the table onto the stack and encode it
                LuaAPI.lua_getref(L, luaReference);
                if (0 == LuaAPI.xlua_cjson_encode(L, -1))
                {
                    // Success - pcall returns 0 on success, string is at top
                    string json = LuaAPI.lua_tostring(L, -1);
                    LuaAPI.lua_settop(L, oldTop);
                    return json;
                }
                else
                {
                    // Error - pcall returns non-zero on error
                    string err = LuaAPI.lua_tostring(L, -1);
                    LuaAPI.lua_settop(L, oldTop);
                    throw new Exception("cjson encode error: " + err);
                }
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }
    }
}
