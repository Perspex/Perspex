//
//  AvnString.m
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 07/11/2018.
//  Copyright © 2018 Avalonia. All rights reserved.
//

#include "common.h"
#include <vector>

class AvnStringImpl : public virtual ComSingleObject<IAvnString, &IID_IAvnString>
{
private:
    int _length;
    const char* _cstring;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnStringImpl(NSString* string)
    { 
        auto cstring = [string cStringUsingEncoding:NSUTF8StringEncoding];
        _length = (int)[string lengthOfBytesUsingEncoding:NSUTF8StringEncoding];
        
        _cstring = (const char*)malloc(_length + 5);
        
        memset((void*)_cstring, 0, _length + 5);
        memcpy((void*)_cstring, (void*)cstring, _length);
    }
    
    AvnStringImpl(void*ptr, int len)
    {
        _length = len;
        _cstring = (const char*)malloc(_length);
        memcpy((void*)_cstring, ptr, len);
    }
    
    virtual ~AvnStringImpl()
    {
        free((void*)_cstring);
    }
    
    virtual HRESULT Pointer(void**retOut) override
    {
        @autoreleasepool
        {
            if(retOut == nullptr)
            {
                return E_POINTER;
            }
            
            *retOut = (void*)_cstring;
            
            return S_OK;
        }
    }
    
    virtual HRESULT Length(int*retOut) override
    {
        if(retOut == nullptr)
        {
            return E_POINTER;
        }
        
        *retOut = _length;
        
        return S_OK;
    }
};

class AvnStringArrayImpl : public virtual ComSingleObject<IAvnStringArray, &IID_IAvnStringArray>
{
private:
    std::vector<ComPtr<IAvnString>> _list;
public:
    FORWARD_IUNKNOWN()
    AvnStringArrayImpl(NSArray<NSString*>* array)
    {
        for(int c = 0; c < [array count]; c++)
        {
            ComPtr<IAvnString> s;
            *s.getPPV() = new AvnStringImpl([array objectAtIndex:c]);
            _list.push_back(s);
        }
    }
    
    AvnStringArrayImpl(NSArray<NSURL*>* array)
    {
        for(int c = 0; c < [array count]; c++)
        {
            ComPtr<IAvnString> s;
            *s.getPPV() = new AvnStringImpl([array objectAtIndex:c].absoluteString);
            _list.push_back(s);
        }
    }
    
    AvnStringArrayImpl(NSString* string)
    {
        ComPtr<IAvnString> s;
        *s.getPPV() = new AvnStringImpl(string);
        _list.push_back(s);
    }
    
    virtual unsigned int GetCount() override
    {
        return (unsigned int)_list.size();
    }
    
    virtual HRESULT Get(unsigned int index, IAvnString**ppv) override
    {
        if(_list.size() <= index)
            return E_INVALIDARG;
        *ppv = _list[index].getRetainedReference();
        return S_OK;
    }
};

IAvnString* CreateAvnString(NSString* string)
{
    return new AvnStringImpl(string);
}


IAvnStringArray* CreateAvnStringArray(NSArray<NSString*> * array)
{
    return new AvnStringArrayImpl(array);
}

IAvnStringArray* CreateAvnStringArray(NSArray<NSURL*> * array)
{
    return new AvnStringArrayImpl(array);
}

IAvnStringArray* CreateAvnStringArray(NSString* string)
{
    return new AvnStringArrayImpl(string);
}

IAvnString* CreateByteArray(void* data, int len)
{
    return new AvnStringImpl(data, len);
}

NSString* GetNSStringAndRelease(IAvnString* s)
{
    if (s != nullptr)
    {
        char* p;
        
        if (s->Pointer((void**)&p) == S_OK)
        {
            return [NSString stringWithUTF8String:p];
        }
        
        s->Release();
    }
    
    return nullptr;
}
