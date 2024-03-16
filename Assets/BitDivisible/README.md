# BitDivisible

## overview
automatically implement bit shift properties.

## environment

#### Unity
- 2022.3.12f1 (or later)

#### Source Generator
- .NET Standard 2.0
- Microsoft.CodeAnalysis.CSharp 4.3.1

## UPM
<pre>https://github.com/riru-ruribe/BitDivisible.git?path=Assets/BitDivisible</pre>

## detail

```C#
[BitDivisible]
partial class Data
{
    [BitDivisibleField(typeof(int), 3, "A")]
    [BitDivisibleField(typeof(bool), 1, "B")]
    [BitDivisibleField(typeof(int), 2, "C")]
    ulong i;
}

// ↓ auto implement

partial class Data
{
    public int A => (int)(i & 0b111);
    public bool B => ((i >> 3) & 0b1) == 1;
    public int C => (int)((i >> 4) & 0b11);
}
```
