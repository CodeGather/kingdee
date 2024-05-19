using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.Log;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using System;

/// 违反了继承安全性规则。派生类型必须与基类型的安全可访问性匹配或者比基类型的安全可访问性低。
[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]


namespace Jokui.Sean.Rao.SaleOrder.shopSn
{
    /// <summary>
    /// 【服务插件】单据编号保存到单据体上
    /// </summary>
    /// [Description("单据编号保存到单据体上-服务插件"), HotUpdate]
    [Description("1[服务插件]单据编号和其他数据保存到单据体字段上"), HotUpdate]
    public class CopyBillNoToEntityOperationServicePlugIn : AbstractOperationServicePlugIn
    {
        public override void OnPrepareOperationServiceOption(OnPrepareOperationServiceEventArgs e)
        {
            base.OnPrepareOperationServiceOption(e);
            if (!string.IsNullOrWhiteSpace(this.FormOperation.LoadKeys) && this.FormOperation.LoadKeys != "null")
            {
                // 设置操作完后刷新动态的字段
                var loadKeys = KDObjectConverter.DeserializeObject<List<string>>(this.FormOperation.LoadKeys);
                if (loadKeys == null)
                {
                    loadKeys = new List<string>();
                }
                // 客户商场/门店
                if (!loadKeys.Contains("F_LTL_DetailLocName"))
                {
                    loadKeys.Add("F_LTL_DetailLocName");
                }
                // 店铺订单编号
                if (!loadKeys.Contains("F_LTL_DetailLocCode"))
                {
                    loadKeys.Add("F_LTL_DetailLocCode");
                }
                this.FormOperation.LoadKeys = KDObjectConverter.SerializeObject(loadKeys);
            }
        }
        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            base.BeginOperationTransaction(e);
            //保存单据时， 将单据编号保存到单据体字段上（F_LTL_DetailLocCode）
            var billNoField = this.BusinessInfo.GetBillNoField();
            /// 信誓旦旦实体
            var entity = this.BusinessInfo.GetEntity("FSaleOrderEntry");
            /// 店铺订单号字段
            var codeField = this.BusinessInfo.GetField("F_LTL_DetailLocCode");
            /// 客户商场/门店
            var nameField = this.BusinessInfo.GetField("F_LTL_DetailLocName");
            // 写上机操作日志
            var allRows = new StringBuilder();
            Logger.Info("BOS", "日志开始");
            foreach (DynamicObject dataEntity in e.DataEntitys)
            {
                // 1. 获取单据体行集合  
                var rows = (DynamicObjectCollection)dataEntity[entity.DynamicProperty.Name];
                if (rows != null)
                {
                    // 3. 获取被编辑的行
                    Hashtable editMap = new Hashtable();
                    for (int i = 0; i < rows.UpdateRows.Count; i++)
                    {
                        DynamicObject row = rows.UpdateRows[i];
                        allRows.Append(string.Format("外层在编辑列表的数据：\n{0:G}.{1:G}-{2:G}\n", i, row[nameField.DynamicProperty], row[codeField.DynamicProperty]));
                        /// 获取单据体的内构id
                        long entityId = Convert.ToInt64(row[0]);
                        /// 将编辑的行进行归类用于后期使用过滤掉该数据                    
                        if (row.DataEntityState.FromDatabase == true && !editMap.ContainsKey(entityId))
                        {
                            editMap.Add(entityId, true);
                            // 内码
                            allRows.Append(string.Format("在编辑列表的数据：\n{0:G}.{1:G}-{2:G}\n", i, row[nameField.DynamicProperty], entityId));
                        }
                    }
                    Hashtable allMap = new Hashtable();
                    List<int> allNumList = new List<int> { 0 };
                    // 处理全部行的数据
                    for (int i = 0; i < rows.Count; i++)
                    {
                        DynamicObject row = rows[i];
                        string code = (string)row[codeField.PropertyName];
                        code = code != null ? code.Trim() : "";
                        string name = (string)row[nameField.PropertyName];
                        name = name != null ? name.Trim() : "";
                        long entityId = Convert.ToInt64(row[0]);
                        /// 不在修改的数据中我们需要处理他的数据，以及处理他的数字部分，过滤掉编辑的数据就是不做改动的数据
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(code) && !allMap.ContainsKey(name) && !editMap.ContainsKey(entityId))
                        {
                            allMap.Add(name, code);
                            /// 符合我们名称的要求时我们将其放入数字列队用于计算我们的组合数字部分
                            allRows.Append(string.Format("不在修改列表的数据：\n{0:G}.{1:G}-{2:G}\n", i, name, code));
                        }
                        /// 需要江有码的数据进行数组的建立
                        if (!string.IsNullOrEmpty(code) && code.Contains("-"))
                        {
                            string[] codeList = code.SplitRegex("-");
                            /// 分割后大于1的情况下我们才将其放入
                            if (codeList.Length > 1)
                            {
                                allNumList.Add(int.Parse(codeList[1]));
                            }
                        }
                    }
                    /// 开始改写数据
                    for (int i = 0; i < rows.Count; i++)
                    {
                        DynamicObject row = rows[i];
                        long entityId = Convert.ToInt64(row[0]);
                        string code = (string)row[codeField.PropertyName];
                        string name = (string)row[nameField.PropertyName];
                        string newCode = "";
                        name = name != null ? name.Trim() : "";
                        /// 不在处理的列表时说明他的值被修改我们需要重新处理他的编号，其他的忽略
                        if (!string.IsNullOrEmpty(name))
                        {
                            string hasCodeData = (string)allMap[name];
                            hasCodeData = hasCodeData != null ? hasCodeData.Trim() : "";
                            /// 修改的数据集合
                            /// 将不再未改动数据列中的数据并且在未改动数据中但是编码为空的数据进行重新生成
                            if (!allMap.ContainsKey(name) || (allMap.ContainsKey(name) && string.IsNullOrEmpty(hasCodeData)))
                            {
                                int maxVal = allNumList.Max() + 1;
                                allNumList.Add(maxVal);
                                newCode = dataEntity[billNoField.PropertyName] + "-" + maxVal;
                                allMap[name] = newCode;
                                allRows.Append(string.Format("新插新生成的数据：\n{0:G}.{1:G}-{2:G}\n", i, name, newCode, code));
                            }
                            /// 剩余的都是有的数据
                            else
                            {
                                newCode = hasCodeData;
                                allRows.Append(string.Format("未改动的数据：\n{0:G}.{1:G}-{2:G}-{3:G}\n", i, name, code, string.IsNullOrEmpty(hasCodeData)));
                            }
                        }
                        row[codeField.PropertyName] = newCode;
                    }
                    // 2. 获取被插入的行  
                    for (int i = 0; i < rows.InsertRows.Count; i++)
                    {
                        DynamicObject row = rows.InsertRows[i];
                        // 判断此行，是不是从数据库读取出来，然后被删除了                     
                        // 用户可能会在界面上，点新增行，随后点删除行，这种数据行，不需要关注                    
                        if (row.DataEntityState.FromDatabase == true)
                        {
                            allRows.Append(string.Format("新插入列表的数据：\n{0:G}.{1:G}-{2:G}\n", i, row[nameField.PropertyName], row[codeField.PropertyName]));
                        }
                    }
                }
            }
            Logger.Info("ALL", allRows.GetString());
            Logger.Info("BOS", "日志结束");
        }
    }
}