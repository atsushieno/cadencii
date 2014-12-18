using System;
using System.Windows.Forms;
using NUnit.Framework;
using cadencii;
using Cadencii.Application.Forms;

namespace cadencii
{
    [TestFixture]
    public class ExceptionNotifyFormControllerTest : ExceptionNotifyFormController
    {
	public ExceptionNotifyFormControllerTest ()
		: base (c => new ExceptionNotifyFormUiImpl(c))
	{
	}
        [Test]
        public void testSetReportTarget()
        {
            string expected = "OSVersion=" + Environment.OSVersion.ToString() + "\ndotNetVersion=" + System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion();
            Assert.AreEqual(expected, base.getSystemInfo());
        }

        [Test]
        public void testExtractMessageString()
        {
            Exception ex = this.prepareException();
            string actual = base.extractMessageString(ex, 0);
            Assert.AreEqual("[exception-0]\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n", actual);
        }

        //[Test]
        public void test()
        {
            this.setReportTarget(this.prepareException());
            this.getUi().ShowDialog(null);
        }

        private Exception prepareException()
        {
            try {
                int.Parse("XYZ");
            } catch (Exception ex) {
                return ex;
            }
            return null;
        }
    }
}
