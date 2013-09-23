﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Expect;
using System.Threading.Tasks;
using System.Threading;

namespace Expect.Test
{
    [TestClass]
    public class SpawnTest
    {
        [TestMethod]
        public void SendTest()
        {
            var proc = new Mock<Process>();
            Spawn spawn = new Spawn(proc.Object);
            string command = "test command";

            spawn.send(command);

            proc.Verify(p => p.write(command));
        }

        [TestMethod]
        public async Task BasicExpectTest()
        {
            var proc = new Mock<Process>();
            proc.Setup(p => p.readAsync()).Returns(ReturnStringAfterDelay("test expected string test", 10));
            Spawn spawn = new Spawn(proc.Object);
            bool funcCalled = false;

            await spawn.expect("expected string", () => funcCalled = true);

            Assert.IsTrue(funcCalled);
        }

        [TestMethod]
        public async Task BasicExpectWithOutputTest()
        {
            var proc = new Mock<Process>();
            proc.Setup(p => p.readAsync()).Returns(ReturnStringAfterDelay("test expected string test", 10));
            Spawn spawn = new Spawn(proc.Object);
            bool funcCalled = false;

            string output = "";
            await spawn.expect("expected string", (s) => {funcCalled = true; output = s;});

            Assert.IsTrue(funcCalled);
            Assert.AreEqual("test expected string test", output);
        }

        [TestMethod]
        public async Task SplitResultExpectTest()
        {
            var proc = new Mock<Process>();
            int i = 0;
            Task<string>[] tasks = {ReturnStringAfterDelay("test expected ", 100), 
                                     ReturnStringAfterDelay("string test", 150)};
            proc.Setup(p => p.readAsync()).Returns(() => tasks[i]).Callback(() => i++);
            Spawn spawn = new Spawn(proc.Object);
            bool funcCalled = false;

            await spawn.expect("expected string", () => funcCalled = true);

            Assert.IsTrue(funcCalled);
            Assert.AreEqual(2, i);
        }

        [TestMethod]
        public async Task SplitResultExpectWitOutputTest()
        {
            var proc = new Mock<Process>();
            int i = 0;
            Task<string>[] tasks = {ReturnStringAfterDelay("test expected ", 100), 
                                     ReturnStringAfterDelay("string test", 150)};
            proc.Setup(p => p.readAsync()).Returns(() => tasks[i]).Callback(() => i++);
            Spawn spawn = new Spawn(proc.Object);
            bool funcCalled = false;
            string output = "";

            await spawn.expect("expected string", (s) => { funcCalled = true; output = s; });

            Assert.IsTrue(funcCalled);
            Assert.AreEqual(2, i);
            Assert.AreEqual("test expected string test", output);
        }

        [TestMethod]
        public async Task SendResetOutputTest()
        {
            var proc = new Mock<Process>();
            int i = 0;
            Task<string>[] tasks = {ReturnStringAfterDelay("test expected ", 100), 
                                     ReturnStringAfterDelay("string test", 150),
                                   ReturnStringAfterDelay("next expected string", 100)};
            proc.Setup(p => p.readAsync()).Returns(() => tasks[i]).Callback(() => i++);
            Spawn spawn = new Spawn(proc.Object);
            string output = "";

            await spawn.expect("expected string", (s) => { spawn.send("test");});
            await spawn.expect("next expected", (s) => { output = s; });
            Assert.AreEqual("next expected string", output);
        }

        private async Task<string> ReturnStringAfterDelay(string s, int delayInMs)
        {
            await Task.Delay(delayInMs);
            return s;
        }

        [TestMethod]
        public async Task TimeoutThrownExpectTest()
        {
            var proc = new Mock<Process>();
            proc.Setup(p => p.readAsync()).Returns(ReturnStringAfterDelay("test expected string test", 1200));
            Spawn spawn = new Spawn(proc.Object);
            spawn.setTimeout(500);
            Exception exc = null;
            bool funcCalled = false;

            try
            {
                await spawn.expect("expected string", () => funcCalled = true);
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.IsNotNull(exc);
            Assert.IsInstanceOfType(exc, typeof(TimeoutException));
            Assert.IsFalse(funcCalled);
        }

        [TestMethod]
        public async Task TimeoutNotThrownExpectTest()
        {
            var proc = new Mock<Process>();
            proc.Setup(p => p.readAsync()).Returns(ReturnStringAfterDelay("test expected string test", 1200));
            Spawn spawn = new Spawn(proc.Object);
            spawn.setTimeout(2400);
            Exception exc = null;
            bool funcCalled = false;

            try
            {
                await spawn.expect("expected string", () => funcCalled = true);
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.IsNull(exc);
            Assert.IsTrue(funcCalled);
        }

        [TestMethod]
        public void SetGetTimeout2400Test()
        {
            var proc = new Mock<Process>();
            Spawn spawn = new Spawn(proc.Object);
            spawn.setTimeout(2400);
            Assert.AreEqual(2400, spawn.getTimeout());
        }

        [TestMethod]
        public void SetGetTimeout200Test()
        {
            var proc = new Mock<Process>();
            Spawn spawn = new Spawn(proc.Object);
            spawn.setTimeout(200);
            Assert.AreEqual(200, spawn.getTimeout());
        }

        [TestMethod]
        public void SetGetTimeoutIncorrectValueTest()
        {
            var proc = new Mock<Process>();
            Spawn spawn = new Spawn(proc.Object);
            Exception exc = null;
            ArgumentOutOfRangeException aoorexc = null;
            try
            {
                spawn.setTimeout(-1);
            }
            catch (ArgumentOutOfRangeException aoore)
            {
                aoorexc = aoore;
            }
            catch (Exception e)
            {
                exc = e;
            }

            Assert.IsNull(exc);
            Assert.IsNotNull(aoorexc);
            Assert.AreEqual("timeout", aoorexc.ParamName);
        }
    }
}
