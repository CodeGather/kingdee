using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;

namespace Jac.XkDemo.BOS.Business.PlugIn
{
    /// <summary>
    /// 【表单插件】值更新事件之更新自己
    /// </summary>
    [Description("【表单插件】值更新事件之更新自己"), HotUpdate]
    public class DataChangedEventInvokeSelfFormPlugIn : AbstractDynamicFormPlugIn
    {
        public override void EntryBarItemClick(BarItemClickEventArgs e)
        {
            base.EntryBarItemClick(e);
            if (e.BarItemKey.Equals("F_LTL_DetailLocName"))
            {
                this.Model.DataObject["F_LTL_DetailLocCode"] = "";
                this.View.UpdateView("F_LTL_DetailLocCode");
                this.View.UpdateView("F_LTL_DetailLocName");
            }
        }
    }
}