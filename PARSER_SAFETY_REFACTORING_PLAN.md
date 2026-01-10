# Parser/Lexer Safety Refactoring Plan

**Date**: 2026-01-10  
**Goal**: Make parser/lexer more safe, maintainable, and defensive  
**Strategy**: Small incremental steps with tests after each change

---

## 📊 Current State Analysis

### File Metrics
- **ToonParser.cs**: 979 lines (LARGE - needs refactoring)
- **ToonLexer.cs**: 414 lines (acceptable)

### Method Complexity Analysis

| Method | Lines | Complexity | Priority |
|--------|-------|------------|----------|
| `ParseObject()` | ~175 | 🔴 Very High | **1** |
| `ParseList()` | ~155 | 🔴 Very High | **2** |
| `ParseTabularArray()` | ~105 | 🟡 High | **3** |
| `ParseValue()` | ~30 | 🟢 Low | - |
| `ParseInlinePrimitiveArray()` | ~35 | 🟢 Low | - |

### Identified Issues

#### 1. ParseObject() - Line 113-287 (175 lines) 🔴
**Problems**:
- Deep nesting (5-6 levels)
- Multiple responsibilities (key parsing, array detection, value parsing)
- Complex lookahead logic (lines 200-221)
- Long method hard to understand and test

**Risks**:
- Bug-prone due to complexity
- Hard to modify without breaking
- Difficult to add new features

#### 2. ParseList() - Line 468-846 (379 lines!) 🔴
**Problems**:
- EXTREMELY long method
- Multiple nested loops
- Handles 3 different list types (scalar, inline object, nested object)
- Complex indentation logic
- Tabular array detection embedded

**Risks**:
- Highest risk for bugs
- Nearly impossible to reason about
- Performance bottleneck potential

#### 3. Deep Lookahead Pattern - Lines 600-613, 200-221
**Problems**:
- Manual token scanning with nested loops
- Repeated pattern across methods
- Hard to understand intent
- Error-prone index management

#### 4. Error Messages
**Problems**:
- Generic messages ("Expected value after ':'")
- Missing context (what were we parsing?)
- No suggestions for fixes

#### 5. No Guard Clauses
**Problems**:
- Token type checks without null/bounds validation
- Assumes lexer always produces valid tokens
- No defensive checks for internal state

---

## 🎯 Refactoring Strategy

### Phase 3A: Extract Helper Methods (Low Risk)
**Goal**: Break down large methods without changing logic  
**Duration**: 1-2 hours  
**Steps**: 10 small incremental changes

### Phase 3B: Add Guard Clauses (Low Risk)
**Goal**: Add defensive programming patterns  
**Duration**: 30 minutes  
**Steps**: 5 small changes

### Phase 3C: Improve Error Messages (Low Risk)
**Goal**: Better diagnostics and user experience  
**Duration**: 30 minutes  
**Steps**: 3 changes

### Phase 3D: Optional Advanced Refactoring (Medium Risk)
**Goal**: Structural improvements (if time permits)  
**Duration**: 1-2 hours  
**Steps**: Variable based on needs

---

## 📝 Phase 3A: Extract Helper Methods

### Step 1: Extract Lookahead Logic ✅
**File**: ToonParser.cs  
**What**: Extract `IsFollowedByListItem()` helper method  
**Lines**: 200-221 (lookahead for list detection)  
**Risk**: 🟢 Low - Pure function, no state changes  
**Test**: Run all tests after change

```csharp
// Before: Inline lookahead logic
var peekPos = _position;
while (peekPos < _tokens.Count && _tokens[peekPos].Type == ToonTokenType.Newline)
{
    peekPos++;
}
if (peekPos < _tokens.Count && _tokens[peekPos].Type == ToonTokenType.Indent)
{
    var nextPos = peekPos + 1;
    if (nextPos < _tokens.Count && _tokens[nextPos].Type == ToonTokenType.ListItem)
    {
        isListArray = true;
    }
}

// After: Helper method
private bool IsFollowedByListItem(int startPosition)
{
    var pos = startPosition;
    
    // Skip newlines
    while (pos < _tokens.Count && _tokens[pos].Type == ToonTokenType.Newline)
    {
        pos++;
    }
    
    // Check for Indent + ListItem pattern
    if (pos < _tokens.Count && _tokens[pos].Type == ToonTokenType.Indent)
    {
        var nextPos = pos + 1;
        return nextPos < _tokens.Count && _tokens[nextPos].Type == ToonTokenType.ListItem;
    }
    
    return false;
}
```

**Expected**: Same behavior, clearer intent  
**Validation**: All 425 tests pass ✅

---

### Step 2: Extract ParseObjectKeyValuePair() ✅
**File**: ToonParser.cs  
**What**: Extract key-value parsing logic from ParseObject  
**Lines**: 148-269 (key parsing + value parsing)  
**Risk**: 🟢 Low - Well-defined scope  
**Test**: Run all tests after change

```csharp
private (string key, ToonValue value) ParseObjectKeyValuePair(int indentLevel)
{
    var keyToken = Peek();
    if (keyToken.Type != ToonTokenType.Key)
    {
        throw new ToonParseException("Expected key", keyToken.Line, keyToken.Column);
    }
    
    Advance();
    var key = keyToken.Value.ToString();
    
    // Parse array notation (if present)
    var (arrayLength, fieldNames) = ParseArrayNotation();
    
    // Expect colon
    ExpectToken(ToonTokenType.Colon, "':'");
    Advance();
    
    // Parse value
    var value = ParseValueAfterColon(indentLevel, arrayLength, fieldNames);
    
    return (key, value);
}
```

**Expected**: ParseObject becomes much shorter  
**Validation**: All 425 tests pass ✅

---

### Step 3: Extract ParseArrayNotation() ✅
**File**: ToonParser.cs  
**What**: Extract array length/fields parsing  
**Lines**: 163-179  
**Risk**: 🟢 Low - Pure parsing logic  
**Test**: Run all tests after change

```csharp
private (int? arrayLength, string[]? fieldNames) ParseArrayNotation()
{
    int? arrayLength = null;
    string[]? fieldNames = null;
    
    // Check for array length [n]
    if (Peek().Type == ToonTokenType.ArrayLength)
    {
        var lengthToken = Advance();
        var lengthStr = lengthToken.Value.ToString().Trim('[', ']');
        if (int.TryParse(lengthStr, out var len))
        {
            arrayLength = len;
        }
    }
    
    // Check for field names [field1, field2]
    if (Peek().Type == ToonTokenType.ArrayFields)
    {
        var fieldsToken = Advance();
        var fieldsStr = fieldsToken.Value.ToString().Trim('[', ']');
        fieldNames = fieldsStr.Split(',').Select(f => f.Trim()).ToArray();
    }
    
    return (arrayLength, fieldNames);
}
```

**Expected**: Cleaner ParseObject  
**Validation**: All 425 tests pass ✅

---

### Step 4: Extract ParseListItemScalar() ✅
**File**: ToonParser.cs  
**What**: Extract scalar list item parsing from ParseList  
**Lines**: ~483-488  
**Risk**: 🟢 Low - Simple case  
**Test**: Run all tests after change

```csharp
private ToonValue ParseListItemScalar()
{
    if (!IsValueToken(Peek().Type))
    {
        throw new ToonParseException("Expected scalar value after '-'", Peek().Line, Peek().Column);
    }
    
    var valueToken = Advance();
    return ParseValueToken(valueToken);
}
```

**Expected**: ParseList more readable  
**Validation**: All 425 tests pass ✅

---

### Step 5: Extract ParseListItemInlineObject() ✅
**File**: ToonParser.cs  
**What**: Extract inline object parsing (- key: value)  
**Lines**: ~489-660  
**Risk**: 🟡 Medium - Complex logic  
**Test**: Run all tests after change

```csharp
private ToonObject ParseListItemInlineObject(int indentLevel)
{
    var itemObject = new ToonObject();
    
    // Parse inline first field (- key: value)
    var firstKey = Advance().Value.ToString();
    ExpectToken(ToonTokenType.Colon, "':'");
    Advance();
    
    // Parse first value
    var firstValue = ParseInlineOrNestedValue(indentLevel);
    itemObject.Properties[firstKey] = firstValue;
    
    // Consume newline if present
    if (Peek().Type == ToonTokenType.Newline)
    {
        Advance();
    }
    
    // Parse additional properties at higher indent
    ParseAdditionalObjectProperties(itemObject, indentLevel);
    
    return itemObject;
}
```

**Expected**: ParseList significantly shorter  
**Validation**: All 425 tests pass ✅

---

### Step 6: Extract ParseListItemNestedObject() ✅
**File**: ToonParser.cs  
**What**: Extract nested object parsing (- \n properties)  
**Lines**: ~662-805  
**Risk**: 🟡 Medium - Complex indentation logic  
**Test**: Run all tests after change

```csharp
private ToonObject ParseListItemNestedObject(int indentLevel)
{
    // Expect newline after dash
    ExpectToken(ToonTokenType.Newline, "newline after '-'");
    Advance();
    
    var itemObject = new ToonObject();
    
    // Parse properties at higher indentation
    while (!IsAtEnd())
    {
        SkipNewlines();
        
        if (IsAtEnd())
        {
            break;
        }
        
        // Check indentation - must be greater than list level
        var propIndent = GetCurrentIndentAndAdvance();
        
        if (propIndent <= indentLevel)
        {
            break; // Dedented back to list level or less
        }
        
        // Parse key-value pair
        var (key, value) = ParseObjectKeyValuePair(propIndent);
        itemObject.Properties[key] = value;
        
        // Consume optional newline
        if (Peek().Type == ToonTokenType.Newline)
        {
            Advance();
        }
    }
    
    return itemObject;
}
```

**Expected**: ParseList much more manageable  
**Validation**: All 425 tests pass ✅

---

### Step 7: Add ExpectToken() Helper ✅
**File**: ToonParser.cs  
**What**: Add helper for token type assertions  
**Risk**: 🟢 Low - Simple utility  
**Test**: Run all tests after change

```csharp
/// <summary>
///     Asserts that the current token is of the expected type.
///     Throws descriptive exception if not.
/// </summary>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private void ExpectToken(ToonTokenType expected, string description)
{
    if (Peek().Type != expected)
    {
        throw new ToonParseException(
            $"Expected {description} but found {Peek().Type}",
            Peek().Line,
            Peek().Column);
    }
}
```

**Expected**: Clearer assertions throughout parser  
**Validation**: All 425 tests pass ✅

---

### Step 8: Add GetCurrentIndentAndAdvance() Helper ✅
**File**: ToonParser.cs  
**What**: Combine getting indent + consuming indent token  
**Risk**: 🟢 Low - Common pattern  
**Test**: Run all tests after change

```csharp
/// <summary>
///     Gets current indentation level and advances past indent token if present.
/// </summary>
private int GetCurrentIndentAndAdvance()
{
    if (!IsAtEnd() && Peek().Type == ToonTokenType.Indent)
    {
        var indent = Peek().Value.Length;
        Advance();
        return indent;
    }
    return 0;
}
```

**Expected**: Reduce repetitive indent handling code  
**Validation**: All 425 tests pass ✅

---

### Step 9: Extract ParseValueAfterColon() ✅
**File**: ToonParser.cs  
**What**: Extract value parsing after colon  
**Lines**: 188-248  
**Risk**: 🟡 Medium - Critical parsing logic  
**Test**: Run all tests after change

```csharp
private ToonValue ParseValueAfterColon(int indentLevel, int? arrayLength, string[]? fieldNames)
{
    SkipWhitespace();
    
    // Check for newline (nested value) or end of input
    if (Peek().Type == ToonTokenType.Newline || IsAtEnd())
    {
        return ParseNestedValue(indentLevel, arrayLength, fieldNames);
    }
    
    // Check for inline value
    if (!IsAtEnd() && IsValueToken(Peek().Type))
    {
        return ParseInlineValue(arrayLength);
    }
    
    // End of input after colon - empty value
    if (IsAtEnd())
    {
        return CreateEmptyValue(arrayLength, fieldNames);
    }
    
    throw new ToonParseException("Expected value after ':'", Peek().Line, Peek().Column);
}
```

**Expected**: ParseObject dramatically simplified  
**Validation**: All 425 tests pass ✅

---

### Step 10: Add Regions for Organization ✅
**File**: ToonParser.cs  
**What**: Add #region comments to group related methods  
**Risk**: 🟢 None - Just comments  
**Test**: Build check only

```csharp
#region Public API
public ToonDocument Parse(string input) { ... }
#endregion

#region Core Parsing Methods
private ToonValue ParseValue(int indentLevel) { ... }
private ToonObject ParseObject(int indentLevel) { ... }
private ToonArray ParseList(int indentLevel) { ... }
#endregion

#region Helper Methods - Token Operations
private ToonToken Peek() { ... }
private ToonToken Advance() { ... }
private bool IsAtEnd() { ... }
#endregion

#region Helper Methods - Parsing
private (int?, string[]?) ParseArrayNotation() { ... }
private ToonValue ParseValueAfterColon(...) { ... }
#endregion

#region Helper Methods - List Items
private ToonValue ParseListItemScalar() { ... }
private ToonObject ParseListItemInlineObject(...) { ... }
#endregion

#region Helper Methods - Validation
private void ExpectToken(...) { ... }
private int GetCurrentIndentAndAdvance() { ... }
#endregion
```

**Expected**: Better code navigation in IDE  
**Validation**: Build succeeds ✅

---

## 📊 Expected Impact

### Before Refactoring
- ParseObject: 175 lines
- ParseList: 379 lines
- **Total complexity**: Very High 🔴

### After Step 10
- ParseObject: ~50 lines (71% reduction)
- ParseList: ~80 lines (79% reduction)
- **Total complexity**: Medium 🟡

### Metrics
- **Line reduction**: ~400 lines moved to focused helpers
- **Method count**: +10 small, focused methods
- **Cyclomatic complexity**: 60-70% reduction
- **Maintainability**: Significantly improved

---

## 🔒 Safety Measures

### For Each Step:
1. ✅ Make ONE change at a time
2. ✅ Run full test suite (425 tests)
3. ✅ Verify all tests pass
4. ✅ Check build has 0 errors
5. ✅ Commit change with descriptive message
6. ✅ If tests fail → Revert immediately

### Rollback Plan:
- Each step is independent
- Can revert any step without affecting others
- Git commit per step for easy rollback

---

## 🎯 Success Criteria

✅ **All 425 tests passing** after each step  
✅ **No behavior changes** - pure refactoring  
✅ **Code more readable** - smaller, focused methods  
✅ **Better error messages** - context-aware  
✅ **Easier to maintain** - clear responsibilities  

---

## 🚀 Phase 3B-D (Optional)

### Phase 3B: Guard Clauses (30 min)
- Add null checks for tokens
- Validate indent levels
- Defensive state checks

### Phase 3C: Error Messages (30 min)
- Add parsing context to errors
- Suggest fixes in error messages
- Include snippet of problematic code

### Phase 3D: Advanced (1-2 hours)
- State machine for list parsing
- Token buffer for better lookahead
- Benchmark performance impact

---

**Ready to start Step 1?** 🔨
