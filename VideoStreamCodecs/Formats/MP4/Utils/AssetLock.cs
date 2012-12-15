//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Media.Formats.MP4
//{

//    // Gives back a lock for the asset guid
//    // example use:
//    //
//    //      lock (AssetLock.Get(guid))
//    //          {
//    //              .....
//    //          }
//    public static class AssetLock
//    {
//        static private Dictionary<string, Lock> AssetLocks = new Dictionary<string, Lock>();

//        class Lock
//        {
//            internal DateTime dt = DateTime.UtcNow;
//        }

//        static object MyLock = new object();
        
//        public static object GetLock(string guid)
//        {
//            // One at a time getting or creating the Lock collection
//            lock (MyLock)
//            {
//                Lock theLock;
//                if (AssetLocks.TryGetValue(guid, out theLock))
//                {
//                    theLock.dt = DateTime.UtcNow;
//                    return theLock;
//                }
//                else
//                {
//                    Prune();

//                    theLock = new Lock();
//                    AssetLocks.Add(guid, theLock);
//                    return theLock;
//                }
//            }
//        }

//        // periodically recycle the list - Potential problem: if we try to put locks on MaxLocks different 
//        // Assets within 1 minute (seems pretty remote).
//        static int MaxLocks = 1000;
//        static void Prune()
//        {
//            if (AssetLocks.Count > MaxLocks)
//            {
//                System.TimeSpan ts = TimeSpan.FromMinutes(1);
//                DateTime now = DateTime.UtcNow;

//                // find any locks accessed in the last minute
//                List<KeyValuePair<string, Lock>> kvpList = AssetLocks.
//                    Where<KeyValuePair<string, Lock>>(x => (now - x.Value.dt) < ts).
//                        ToList();
                
//                // Console.WriteLine(string.Format("Prune:  Locks = {0}, Saved = {1}", AssetLocks.Count, kvpList.Count));
                
//                AssetLocks.Clear();

//                // now add back in the recent ones
//                foreach (KeyValuePair<string, Lock> kvp in kvpList)
//                {
//                    AssetLocks.Add(kvp.Key, kvp.Value);
//                }
//            }
//        }
//    }
//}
