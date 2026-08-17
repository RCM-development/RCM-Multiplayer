
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using RCM_Coop;

namespace PimDeWitte.UnityMainThreadDispatcher{
    static class UnityMainThreadDispatcher{
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();

        public static void Update(){
            lock (_executionQueue){
                while (_executionQueue.Count > 0){
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }

        public static void Enqueue(IEnumerator action){
            lock (_executionQueue){
                _executionQueue.Enqueue(() => {
                    CoopManager.coop.StartCoroutine(action);
                });
            }
        }
        public static void Enqueue(Action action)
        {
            Enqueue(ActionWrapper(action));
        }
        public static Task EnqueueAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            void WrappedAction()
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            Enqueue(ActionWrapper(WrappedAction));
            return tcs.Task;
        }
        static IEnumerator ActionWrapper(Action a){
            a();
            yield return null;
        }

    }
}