using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Kingdee.BOS;
using Kingdee.BOS.Util;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
namespace JDSample.ServicePlugIn.Operation
{
    [Description("在单据保存操作插件中，获取增、删、改的单据体行")]
    public class S160810AEDRowOpPlug : AbstractOperationServicePlugIn
    {
        /// 在单据数据，提交到数据库之前触发此事件：                
        /// 需要在执行保存前，数据还没有提交到数据库时，对单据数据包进行分析，得出增、删、改的单据体行               
        /// <summary>         
        /// 在单据数据，提交到数据库之前触发此事件：                 
        /// </summary>         
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            List<Object> addRows = new List<Object>();
            List<Object> editRows = new List<Object>();
            List<Object> delRows = new List<Object>();
            Entity entity = this.BusinessInfo.GetEntity("FPOOrderEntry");
            foreach (var billObj in e.DataEntitys)
            {
                // 1. 获取单据体行集合                 
                DynamicObjectCollection rows = entity.DynamicProperty.GetValue(billObj) as DynamicObjectCollection;
                // 2. 获取被删除的行                 
                foreach (var row in rows.DeleteRows)
                {
                    // 判断此行，是不是从数据库读取出来，然后被删除了                     
                    // 用户可能会在界面上，点新增行，随后点删除行，这种数据行，不需要关注                    
                    if (row.DataEntityState.FromDatabase == true)
                    {
                        delRows.Add(row);
                    }
                }
                // 2. 获取新增的行、修改的行               
                foreach (var row in rows)
                {
                    if (row.DataEntityState.FromDatabase == true)
                    {
                        // 本行是来自于数据库的，属于被修改的历史行                         
                        editRows.Add(row);
                    }
                    else
                    {
                        // 本行不是来自于数据库，应该是用户点新增行创建的                        
                        addRows.Add(row);
                    }
                }
            }
        }
    }
}
