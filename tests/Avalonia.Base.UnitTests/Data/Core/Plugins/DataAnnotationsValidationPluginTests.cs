using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core.Plugins
{
    public class DataAnnotationsValidationPluginTests : IClassFixture<InvariantCultureFixture>
    {
        [Fact]
        public void Should_Match_Property_With_ValidatorAttribute()
        {
            var target = new DataAnnotationsValidationPlugin();
            var data = new Data();

            Assert.True(target.Match(new WeakReference<object>(data), nameof(Data.Between5And10)));
        }

        [Fact]
        public void Should_Match_Property_With_Multiple_ValidatorAttributes()
        {
            var target = new DataAnnotationsValidationPlugin();
            var data = new Data();

            Assert.True(target.Match(new WeakReference<object>(data), nameof(Data.PhoneNumber)));
        }

        [Fact]
        public void Should_Not_Match_Property_Without_ValidatorAttribute()
        {
            var target = new DataAnnotationsValidationPlugin();
            var data = new Data();

            Assert.False(target.Match(new WeakReference<object>(data), nameof(Data.Unvalidated)));
        }

        [Fact]
        public void Produces_Range_BindingNotificationsx()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new DataAnnotationsValidationPlugin();
            var data = new Data();
            var accessor = inpcAccessorPlugin.Start(new WeakReference<object>(data), nameof(data.Between5And10));
            var validator = validatorPlugin.Start(new WeakReference<object>(data), nameof(data.Between5And10), accessor);
            var result = new List<object>();
            
            var errmsg = new RangeAttribute(5, 10).FormatErrorMessage(nameof(Data.Between5And10));

            validator.Subscribe(x => result.Add(x));
            validator.SetValue(3, BindingPriority.LocalValue);
            validator.SetValue(7, BindingPriority.LocalValue);
            validator.SetValue(11, BindingPriority.LocalValue);

            Assert.Equal(new[]
            {
                new BindingNotification(5),
                new BindingNotification(
                    new DataValidationException(errmsg),
                    BindingErrorType.DataValidationError,
                    3),
                new BindingNotification(7),
                new BindingNotification(
                    new DataValidationException(errmsg),
                    BindingErrorType.DataValidationError,
                    11),
            }, result);
        }

        [Fact]
        public void Produces_Aggregate_BindingNotificationsx()
        {
            var inpcAccessorPlugin = new InpcPropertyAccessorPlugin();
            var validatorPlugin = new DataAnnotationsValidationPlugin();
            var data = new Data();
            var accessor = inpcAccessorPlugin.Start(new WeakReference<object>(data), nameof(data.PhoneNumber));
            var validator = validatorPlugin.Start(new WeakReference<object>(data), nameof(data.PhoneNumber), accessor);
            var result = new List<object>();

            validator.Subscribe(x => result.Add(x));
            validator.SetValue("123456", BindingPriority.LocalValue);
            validator.SetValue("abcdefghijklm", BindingPriority.LocalValue);

            Assert.Equal(3, result.Count);
            Assert.Equal(new BindingNotification(null), result[0]);
            Assert.Equal(new BindingNotification("123456"), result[1]);
            var errorResult = (BindingNotification)result[2];
            Assert.Equal(BindingErrorType.DataValidationError, errorResult.ErrorType);
            Assert.Equal("abcdefghijklm", errorResult.Value);
            var exceptions = ((AggregateException)(errorResult.Error)).InnerExceptions;
            Assert.True(exceptions.Any(ex =>
                ex.Message.Contains("The PhoneNumber field is not a valid phone number.")));
            Assert.True(exceptions.Any(ex =>
                ex.Message.Contains("The field PhoneNumber must be a string or array type with a maximum length of '10'.")));

        }

        private class Data
        {
            [Range(5, 10)]
            public int Between5And10 { get; set; } = 5;

            public int Unvalidated { get; set; }

            [Phone]
            [MaxLength(10)]
            public string PhoneNumber { get; set; }
        }
    }
}
