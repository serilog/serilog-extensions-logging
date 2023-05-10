// Copyright 2019 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog.Events;
using System.Collections;

namespace Serilog.Extensions.Logging;

readonly struct SerilogLogValues : IReadOnlyList<KeyValuePair<string, object?>>
{
    // Note, this struct is only used in a very limited context internally, so we ignore
    // the possibility of fields being null via the default struct initialization.

    readonly MessageTemplate _messageTemplate;
    readonly IReadOnlyDictionary<string, LogEventPropertyValue> _properties;
    readonly KeyValuePair<string, object?>[] _values;

    public SerilogLogValues(MessageTemplate messageTemplate, IReadOnlyDictionary<string, LogEventPropertyValue> properties)
    {
        _messageTemplate = messageTemplate ?? throw new ArgumentNullException(nameof(messageTemplate));

        // The dictionary is needed for rendering through the message template
        _properties = properties ?? throw new ArgumentNullException(nameof(properties));

        // The array is needed because the IReadOnlyList<T> interface expects indexed access
        _values = new KeyValuePair<string, object?>[_properties.Count + 1];
        var i = 0;
        foreach (var p in properties)
        {
            _values[i] = new KeyValuePair<string, object?>(p.Key, (p.Value is ScalarValue sv) ? sv.Value : p.Value);
            ++i;
        }
        _values[i] = new KeyValuePair<string, object?>("{OriginalFormat}", _messageTemplate.Text);
    }

    public KeyValuePair<string, object?> this[int index]
    {
        get => _values[index];
    }

    public int Count => _properties.Count + 1;

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, object?>>)_values).GetEnumerator();

    public override string ToString() => _messageTemplate.Render(_properties);

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
}
