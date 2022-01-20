using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VideoPlayer
{
    /// <summary>
    /// ViewModelの共通基底実装を提供します。
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        /// <summary>
        /// プロパティ名から属性を引く辞書。
        /// </summary>
        private readonly Dictionary<string, DataValidityAttribute[]> propertyAttibutes;

        /// <summary>
        /// プロパティ名からエラーメッセージを引く辞書。
        /// </summary>
        private readonly Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();

        /// <summary>
        /// プロパティ名から関連付けられたコマンドを引く辞書。
        /// </summary>
        private readonly Dictionary<string, List<DelegateCommand>> commandToPropertyDependencyDic = new Dictionary<string, List<DelegateCommand>>();

        public ViewModelBase()
        {
            propertyAttibutes = GetType().GetProperties()
                .Select(prop => new KeyValuePair<string, DataValidityAttribute[]>(prop.Name,
                prop.GetCustomAttributes(typeof(DataValidityAttribute), true)
                .Cast<DataValidityAttribute>().ToArray())).Where(kvp => kvp.Value.Length > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            errors = propertyAttibutes.ToDictionary(kvp => kvp.Key, kvp => new List<string>());
        }

        /// <summary>
        /// プロパティの値を設定します。
        /// </summary>
        /// <typeparam name="T">プロパティの値の型。</typeparam>
        /// <param name="p">設定対象のプロパティ。</param>
        /// <param name="value">設定する値。</param>
        /// <param name="propertyName">設定対象のプロパティの名前。呼び出し元の名前と同じ場合は省略できます。</param>
        protected void SetProperty<T>(ref T p, T value, [CallerMemberName] string propertyName = null)
        {
            if (p == null)
            {
                if (value == null)
                {
                    return;
                }
            }
            else
            {
                if (p.Equals(value))
                {
                    return;
                }
            }
            p = value;
            CheckValidity(value, propertyName);
            RaiseCommandCanExecuteChanged(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaiseCommandCanExecuteChanged(string propertyName)
        {
            if (commandToPropertyDependencyDic.TryGetValue(propertyName, out List<DelegateCommand> delegateCommands))
            {
                foreach (DelegateCommand x in delegateCommands)
                {
                    x.RaiseCanExecuteChanged();
                }
            }
        }

        private void CheckValidity<T>(T value, string propertyName)
        {
            if (propertyAttibutes.TryGetValue(propertyName, out DataValidityAttribute[] dataValidityAttributes))
            {
                IEnumerable<DataValidityAttribute> error = dataValidityAttributes.Where(atr => !atr.IsValid(value));
                var message = error.Select(atr => atr.Message).ToList();
                bool changed = message.Count == errors[propertyName].Count;
                errors[propertyName] = message;

                if (changed)
                {
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                }
            }
        }

        /// <summary>
        /// 指定のプロパティにエラーがあるかどうかを表す値を取得します。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        protected bool HasAnyError(string propertyName)
        {
            return errors.TryGetValue(propertyName, out List<string> v) ? v.Count > 0 : false;
        }

        /// <summary>
        /// コマンドを登録します。
        /// </summary>
        /// <param name="delegateCommand">登録するコマンド。</param>
        /// <param name="propertyNames">コマンドの実行可否に影響するプロパティ名。</param>
        protected DelegateCommand RegisterCommand(DelegateCommand delegateCommand, params string[] propertyNames)
        {
            foreach (string prop in propertyNames)
            {
                if (commandToPropertyDependencyDic.ContainsKey(prop))
                {
                    commandToPropertyDependencyDic[prop].Add(delegateCommand);
                }
                else
                {
                    commandToPropertyDependencyDic.Add(prop, new List<DelegateCommand>(new[] { delegateCommand }));
                }
            }
            return delegateCommand;
        }

        /// <summary>
        /// 何らかのプロパティにエラーがあるかどうかを表す値を取得します。
        /// </summary>
        public bool HasErrors => errors.Any(prop => prop.Value.Count > 0);

        /// <summary>
        /// プロパティの値が変更された場合に発生するイベント。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// エラー状態が変更された場合に発生するイベント。
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// 指定のプロパティに発生しているエラーをすべて取得します。
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public IEnumerable GetErrors(string propertyName)
        {
            return errors.TryGetValue(propertyName, out List<string> v) ? v : null;
        }

        /// <summary>
        /// プロパティの変更を通知します。
        /// </summary>
        /// <param name="propertyName"></param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            RaiseCommandCanExecuteChanged(propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        System.Collections.IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            return GetErrors(propertyName);
        }

        protected class DelegateCommand : ICommand
        {
            public Action<object> ExecuteHandler { get; set; }
            public Func<object, bool> CanExecuteHandler { get; set; }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return CanExecuteHandler?.Invoke(parameter) ?? true;
            }

            public void Execute(object parameter)
            {
                ExecuteHandler?.Invoke(parameter);
            }

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// プロパティの値の正当性検証を行う属性の派生元。
        /// </summary>
        protected abstract class DataValidityAttribute : Attribute
        {
            public abstract bool IsValid(object value);

            public abstract string Message { get; }
        }

        /// <summary>
        /// 文字列が32ビット整数であることを要求します。
        /// </summary>
        protected class Int32String : DataValidityAttribute
        {
            public override string Message => "32ビット整数値が必要です";

            public override bool IsValid(object value)
            {
                return value is string str && int.TryParse(str, out int _);
            }
        }

        protected class TimespanString : DataValidityAttribute
        {
            public override string Message => "Timespanと解釈できる文字列が必要です";

            public override bool IsValid(object value)
            {
                return value is string str && TimeSpan.TryParse(str, out _) is true;
            }
        }
    }
}
