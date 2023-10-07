using System;
using NUnit.Framework;
using UnityEngine;

namespace PBUnityMultiplayer.Tests.Runtime.TestUtils
{
    public class WaitUntilWithTimeOut : CustomYieldInstruction
    {
        private readonly Func<bool> _func;
        private readonly float _timeOut;
        public override bool keepWaiting => WaitForCondition() || WaitForTimeOut();

        public WaitUntilWithTimeOut(Func<bool> func, float timeOut)
        {
            _func = func;
            _timeOut = timeOut;
        }
        
        private bool WaitForCondition()
        {
            if (!_func()) return false;
            
            Assert.True(true);
            
            return true;
        }

        private bool WaitForTimeOut()
        {
            if (_timeOut - Time.deltaTime <= 0)
            {
                Assert.Fail("TimeOut");
            }

            return false;
        }
    }
}