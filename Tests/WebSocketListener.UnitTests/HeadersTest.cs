using System.Collections.Specialized;
using vtortola.WebSockets.Http;

namespace vtortola.WebSockets.UnitTests
{
    public class HeadersTest
    {
        [Theory,
        TestCase("Host:This is MyHost", "Host", new[] { "This is MyHost" }),
        TestCase(" Host:This is MyHost  ", "Host", new[] { "This is MyHost" }),
        TestCase(" Host :This is MyHost", "Host", new[] { "This is MyHost" }),
        TestCase(" Host : This is MyHost", "Host", new[] { "This is MyHost" }),
        TestCase(" Host  :  This is MyHost", "Host", new[] { "This is MyHost" }),
        TestCase(" Host  :  This is MyHost ", "Host", new[] { "This is MyHost" }),
        TestCase(" Host  :  This is MyHost   ", "Host", new[] { "This is MyHost" }),
        TestCase("  Host  :  This is MyHost   ", "Host", new[] { "This is MyHost" }),
        TestCase("  Host  :  This is , MyHost   ", "Host", new[] { "This is , MyHost" }), // host is atomic header and should be split
        TestCase("MyCustomHeader:MyValue1,MyValue2", "MyCustomHeader", new[] { "MyValue1", "MyValue2" }),
        TestCase("MyCustomHeader: MyValue1,MyValue2", "MyCustomHeader", new[] { "MyValue1", "MyValue2" }),
        TestCase("MyCustomHeader: MyValue1 ,MyValue2", "MyCustomHeader", new[] { "MyValue1", "MyValue2" }),
        TestCase("MyCustomHeader: MyValue1, MyValue2", "MyCustomHeader", new[] { "MyValue1", "MyValue2" }),
        TestCase("MyCustomHeader: MyValue1 , MyValue2", "MyCustomHeader", new[] { "MyValue1", "MyValue2" }),
        TestCase("MyCustomHeader: MyValue1 , MyValue2 ", "MyCustomHeader", new[] { "MyValue1", "MyValue2" }),
        TestCase("MyCustomHeader: MyValue1 , MyValue2  ", "MyCustomHeader", new[] { "MyValue1", "MyValue2" }),
        TestCase("MyCustomHeader: MyValue1 , MyValue2 , MyValue3", "MyCustomHeader", new[] { "MyValue1", "MyValue2", "MyValue3" }),
        ]
        public void TryParseAndAddRequestHeaderTest(string header, string expectedKey, string[] expectedValues)
        {
            var headers = new Headers<RequestHeader>();

            headers.TryParseAndAdd(header);
            var actualValues = headers.GetValues(expectedKey).ToArray();

            Assert.That(actualValues, Is.EqualTo(expectedValues));
        }

        [Test]
        public void ClearTest()
        {
            // clear filled
            var dict = new Headers<RequestHeader>();
            dict.Set(RequestHeader.Accept, "value");
            dict.Set("Custom1", "value");
            Assert.That(dict.Count, Is.EqualTo(2));
            dict.Clear();
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.False(dict.Contains(RequestHeader.Accept));
            Assert.False(dict.Contains("Accept"));
            Assert.False(dict.Contains("Custom1"));

            // clear new
            dict = new Headers<RequestHeader>();
            dict.Clear();
            dict = new Headers<RequestHeader>(new NameValueCollection());
            dict.Clear();
            dict = new Headers<RequestHeader>(new Dictionary<string, string>());
            dict.Clear();

            // clear empty
            dict = new Headers<RequestHeader>();
            dict.Set(RequestHeader.Trailer, "value");
            dict.Remove(RequestHeader.Trailer);
            dict.Clear();
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.False(dict.Contains(RequestHeader.Trailer));

            // clear cleared
            dict = new Headers<RequestHeader>();
            dict.Clear();
            dict.Clear();
        }
        [Test]
        public void ConstructorTest()
        {
            // empty
            var dict1 = new Headers<RequestHeader>();

            // namevalue collection
            var dict2 = new Headers<RequestHeader>(new NameValueCollection
            {
                {
                    "Custom", "Value1"
                },
                {
                    "Via", "Value2"
                }
            });
            Assert.That(dict2.Count, Is.EqualTo(2));
            Assert.That(dict2["Custom"], Is.EqualTo("Value1"));
            Assert.That(dict2[RequestHeader.Via], Is.EqualTo("Value2"));

            // dictionary
            var dict3 = new Headers<RequestHeader>(new Dictionary<string, string>
            {
                {
                    "Custom", "Value1"
                },
                {
                    "Via", "Value2"
                }
            });
            Assert.That(dict3["Custom"], Is.EqualTo("Value1"));
            Assert.That(dict3[RequestHeader.Via], Is.EqualTo("Value2"));
        }

        [Test]
        public void ContainsTest()
        {
            // contains known
            var dict = new Headers<RequestHeader>();
            var coll = dict as ICollection<KeyValuePair<string, string>>;

            dict.Set(RequestHeader.Via, "value");

            Assert.True(dict.Contains("Via"), "containskey failed");
            Assert.False(dict.Contains("Host"), "containskey failed");
            Assert.True(coll.Contains(new KeyValuePair<string, string>("Via", "value")), "contains(key,value) failed");
            Assert.False(coll.Contains(new KeyValuePair<string, string>("Via", "value1")), "contains(key,value) failed");

            // contains new/missing/existing custom
            dict = new Headers<RequestHeader>();
            coll = dict;

            dict.Set("Custom1", "value");

            Assert.True(dict.Contains("Custom1"), "containskey failed");
            Assert.False(dict.Contains("Custom2"), "containskey failed");
            Assert.True(coll.Contains(new KeyValuePair<string, string>("Custom1", "value")), "contains(key,value) failed");
            Assert.False(coll.Contains(new KeyValuePair<string, string>("Custom1", "value1")), "contains(key,value) failed");

            // contains new/missing/existing mixed
            dict = new Headers<RequestHeader>();
            coll = dict;

            dict.Set(RequestHeader.Via, "value");
            dict.Set("Custom1", "value");

            Assert.True(dict.Contains("Custom1"), "containskey failed");
            Assert.False(dict.Contains("Custom2"), "containskey failed");
            Assert.True(coll.Contains(new KeyValuePair<string, string>("Custom1", "value")), "contains(key,value) failed");
            Assert.False(coll.Contains(new KeyValuePair<string, string>("Custom1", "value1")), "contains(key,value) failed");

            Assert.True(dict.Contains("Via"), "containskey failed");
            Assert.True(dict.Contains(RequestHeader.Via), "contains failed");
            Assert.False(dict.Contains("Host"), "containskey failed");
            Assert.True(coll.Contains(new KeyValuePair<string, string>("Via", "value")), "contains(key,value) failed");
            Assert.False(coll.Contains(new KeyValuePair<string, string>("Via", "value1")), "contains(key,value) failed");

            // contains empty
            dict = new Headers<RequestHeader>();
            coll = dict;

            Assert.False(dict.Contains("Custom2"), "containskey failed");
            Assert.False(coll.Contains(new KeyValuePair<string, string>("Custom1", "value1")), "contains(key,value) failed");
        }

        [Test]
        public void EnumerateTest()
        {
            // enum new/missing/existing known
            var knownDict = new Headers<RequestHeader>();

            knownDict.Set(RequestHeader.Via, "via");
            knownDict.Set(RequestHeader.Trailer, "trailer");
            knownDict.Set(RequestHeader.Te, "te header");
            knownDict.Remove(RequestHeader.Te);

            Assert.That(knownDict.Count, Is.EqualTo(2));

            // enum new/missing/existing custom
            var custDict = new Headers<RequestHeader>();

            custDict.Set("Custom1", "value1");
            custDict.Set("Custom2", "value2");
            custDict.Set("Custom3", "value3");
            custDict.Remove("Custom3");

            Assert.That(custDict.Count, Is.EqualTo(2));

            // enum empty
            var dict = new Headers<RequestHeader>();

            Assert.That(dict.Count, Is.EqualTo(0));

            // get allkeys
            dict = new Headers<RequestHeader>();

            dict.Set(RequestHeader.Via, "via");
            dict.Set(RequestHeader.Trailer, "trailer");
            dict.Set("Custom1", "value1");
            dict.Set("Custom2", "value2");

            var allKeys = (dict as IDictionary<string, string>).Keys.ToList();
            var allValues = (dict as IDictionary<string, string>).Values.ToList();

            var expectedKeys = new[]
            {
                "Via", "Trailer", "Custom1", "Custom2"
            };
            var expectedValues = new[]
            {
                "via", "trailer", "value1", "value2"
            };

            CollectionAssert.AreEquivalent(expectedKeys, allKeys);
            CollectionAssert.AreEquivalent(expectedValues, allValues);
        }

        [Test]
        public void GetSetTest()
        {
            // set new/existing known
            // get missing/existing known
            var dict = new Headers<RequestHeader>();

            dict.Set(RequestHeader.Trailer, "value");
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict[RequestHeader.Trailer], Is.EqualTo("value"));
            Assert.False(dict.Contains(RequestHeader.AcceptCharset));

            dict.Set(RequestHeader.Trailer, "value2");
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict[RequestHeader.Trailer], Is.EqualTo("value2"));
            Assert.False(dict.Contains(RequestHeader.AcceptCharset));

            dict.Set(RequestHeader.Trailer, "valueA");
            dict.Set(RequestHeader.Via, "valueB");
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict[RequestHeader.Trailer], Is.EqualTo("valueA"));
            Assert.That(dict[RequestHeader.Via], Is.EqualTo("valueB"));
            Assert.False(dict.Contains(RequestHeader.AcceptCharset));

            // set new/existing custom
            // get missing/existing custom
            dict = new Headers<RequestHeader>();

            dict.Set("Custom1", "value");
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict["Custom1"], Is.EqualTo("value"));

            dict.Set("Custom1", "value2");
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict["Custom1"], Is.EqualTo("value2"));

            dict.Set("Custom1", "valueA");
            dict.Set("Custom2", "valueB");
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict["Custom1"], Is.EqualTo("valueA"));
            Assert.That(dict["Custom2"], Is.EqualTo("valueB"));
            Assert.False(dict.Contains("Custom3"));

            // get missing/existing custom case-insensitive
            Assert.That(dict["custom1"], Is.EqualTo("valueA"));
            Assert.That(dict["custom2"], Is.EqualTo("valueB"));
            Assert.False(dict.Contains("custom3"));

            // set new/existing mixed
            // get missing/existing mixed
            dict = new Headers<RequestHeader>();
            dict.Set("Custom1", "value");
            dict.Set(RequestHeader.Trailer, "value");
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict["Custom1"], Is.EqualTo("value"));
            Assert.That(dict[RequestHeader.Trailer], Is.EqualTo("value"));

            dict.Set("Custom1", "value2");
            dict.Set(RequestHeader.Trailer, "value2");
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict["Custom1"], Is.EqualTo("value2"));
            Assert.That(dict[RequestHeader.Trailer], Is.EqualTo("value2"));

            dict.Set("Custom1", "valueA");
            dict.Set("Custom2", "valueB");
            dict.Set(RequestHeader.Trailer, "valueA");
            dict.Set(RequestHeader.Via, "valueB");
            Assert.That(dict.Count, Is.EqualTo(4));
            Assert.That(dict[RequestHeader.Trailer], Is.EqualTo("valueA"));
            Assert.That(dict[RequestHeader.Via], Is.EqualTo("valueB"));
            Assert.That(dict["Custom1"], Is.EqualTo("valueA"));
            Assert.That(dict["Custom2"], Is.EqualTo("valueB"));

            // get missing/existing mixed case-insensitive
            Assert.That(dict["custom1"], Is.EqualTo("valueA"));
            Assert.That(dict["custom2"], Is.EqualTo("valueB"));
            Assert.That(dict["trailer"], Is.EqualTo("valueA"));
            Assert.That(dict["via"], Is.EqualTo("valueB"));

            // get empty
            dict = new Headers<RequestHeader>();
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.That(dict.Get(RequestHeader.Via), Is.EqualTo(""));
            Assert.That(dict.Get("Via"), Is.EqualTo(""));
            Assert.That(dict.Get("Custom"), Is.EqualTo(""));
            Assert.False(dict.Contains(RequestHeader.Via));
            Assert.False(dict.Contains("Via"));
            Assert.False(dict.Contains("Custom"));
        }

        [Test]
        public void RemoveTest()
        {
            // remove new/missing/existing known
            var dict = new Headers<RequestHeader>();
            dict.Set(RequestHeader.Accept, "value");
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict[RequestHeader.Accept], Is.EqualTo("value"));
            dict.Remove(RequestHeader.Accept);
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.False(dict.Contains(RequestHeader.Accept));

            //  remove missing value
            dict.Remove(RequestHeader.Accept);
            dict.Remove(RequestHeader.Via);

            //  remove modified value
            dict.Set(RequestHeader.Accept, "value");
            dict.Set("accept", "value2");
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict[RequestHeader.Accept], Is.EqualTo("value2"));
            dict.Remove(RequestHeader.Accept);
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.False(dict.Contains(RequestHeader.Accept));

            // remove new/missing/existing custom
            dict = new Headers<RequestHeader>();
            dict.Set("Custom1", "value");
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict["Custom1"], Is.EqualTo("value"));
            dict.Remove("Custom1");
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.False(dict.Contains("Custom1"));

            //  remove missing value
            dict.Remove("Custom1");
            dict.Remove("Custom2");

            //  remove modified value
            dict.Set("Custom1", "value");
            dict.Set("Custom1", "value2");
            Assert.That(dict.Count, Is.EqualTo(1));
            Assert.That(dict["Custom1"], Is.EqualTo("value2"));
            dict.Remove("Custom1");
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.False(dict.Contains("Custom1"));

            // remove new/missing/existing mixed
            dict = new Headers<RequestHeader>();
            dict.Set("Custom1", "value");
            dict.Set(RequestHeader.Accept, "value");
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict["Custom1"], Is.EqualTo("value"));
            Assert.That(dict[RequestHeader.Accept], Is.EqualTo("value"));
            dict.Remove("Custom1");
            dict.Remove(RequestHeader.Accept);
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.False(dict.Contains("Custom1"));
            Assert.False(dict.Contains(RequestHeader.Accept));

            //  remove missing value
            dict.Remove("Custom1");
            dict.Remove("Custom2");
            dict.Remove(RequestHeader.Accept);
            dict.Remove(RequestHeader.Via);

            //  remove modified value
            dict.Set("Custom1", "value");
            dict.Set("Custom1", "value2");
            dict.Set(RequestHeader.Accept, "value");
            dict.Set(RequestHeader.Accept, "value2");
            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict["Custom1"], Is.EqualTo("value2"));
            Assert.That(dict[RequestHeader.Accept], Is.EqualTo("value2"));
            dict.Remove("Custom1");
            dict.Remove(RequestHeader.Accept);
            Assert.That(dict.Count, Is.EqualTo(0));
            Assert.False(dict.Contains("Custom1"));
            Assert.False(dict.Contains(RequestHeader.Accept));

            // remove non existing
            dict = new Headers<RequestHeader>();
            dict.Remove(RequestHeader.Host);
            dict.Remove("Custom");
        }
    }
}
