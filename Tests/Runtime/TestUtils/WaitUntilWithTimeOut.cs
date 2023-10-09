using System;
using NUnit.Framework;
using UnityEngine;

namespace PBUnityMultiplayer.Tests.Runtime.TestUtils
{
    public class WaitUntilWithTimeOut : CustomYieldInstruction
    {
        private readonly Func<bool> _func;
        private float _timeOut;
        public override bool keepWaiting => WaitForCondition() || WaitForTimeOut();

        public WaitUntilWithTimeOut(Func<bool> func, float timeOut)
        {
            _func = func;
            _timeOut = timeOut;
        }
        
        private bool WaitForCondition()
        {
            if (!_func()) return false;
            
            Assert.True(_func());
            
            return true;
        }

        private bool WaitForTimeOut()
        {
            _timeOut -= Time.deltaTime;
            
            if (_timeOut - Time.deltaTime <= 0)
            {
                Assert.Fail("TimeOut");
            }
            
            return false;
        }
    }
}