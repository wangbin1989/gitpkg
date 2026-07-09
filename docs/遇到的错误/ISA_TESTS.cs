#:sdk Microsoft.NET.Sdk

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;

using ArmAes = System.Runtime.Intrinsics.Arm.Aes;
using X86Aes = System.Runtime.Intrinsics.X86.Aes;

Console.WriteLine("Supported x86 ISAs:");
Console.WriteLine($"  AES:           {X86Aes.IsSupported}");
Console.WriteLine($"  AVX:           {Avx.IsSupported}");
Console.WriteLine($"  AVX2:          {Avx2.IsSupported}");
Console.WriteLine($"  BMI1:          {Bmi1.IsSupported}");
Console.WriteLine($"  BMI2:          {Bmi2.IsSupported}");
Console.WriteLine($"  FMA:           {Fma.IsSupported}");
Console.WriteLine($"  LZCNT:         {Lzcnt.IsSupported}");
Console.WriteLine($"  PCLMULQDQ:     {Pclmulqdq.IsSupported}");
Console.WriteLine($"  POPCNT:        {Popcnt.IsSupported}");
Console.WriteLine($"  SSE:           {Sse.IsSupported}");
Console.WriteLine($"  SSE2:          {Sse2.IsSupported}");
Console.WriteLine($"  SSE3:          {Sse3.IsSupported}");
Console.WriteLine($"  SSE4.1:        {Sse41.IsSupported}");
Console.WriteLine($"  SSE4.2:        {Sse42.IsSupported}");
Console.WriteLine($"  SSSE3:         {Ssse3.IsSupported}");
Console.WriteLine($"  X86Base:       {X86Base.IsSupported}");

Console.WriteLine("Supported x64 ISAs:");
Console.WriteLine($"  AES.X64:       {X86Aes.X64.IsSupported}");
Console.WriteLine($"  AVX.X64:       {Avx.X64.IsSupported}");
Console.WriteLine($"  AVX2.X64:      {Avx2.X64.IsSupported}");
Console.WriteLine($"  BMI1.X64:      {Bmi1.X64.IsSupported}");
Console.WriteLine($"  BMI2.X64:      {Bmi2.X64.IsSupported}");
Console.WriteLine($"  FMA.X64:       {Fma.X64.IsSupported}");
Console.WriteLine($"  LZCNT.X64:     {Lzcnt.X64.IsSupported}");
Console.WriteLine($"  PCLMULQDQ.X64: {Pclmulqdq.X64.IsSupported}");
Console.WriteLine($"  POPCNT.X64:    {Popcnt.X64.IsSupported}");
Console.WriteLine($"  SSE.X64:       {Sse.X64.IsSupported}");
Console.WriteLine($"  SSE2.X64:      {Sse2.X64.IsSupported}");
Console.WriteLine($"  SSE3.X64:      {Sse3.X64.IsSupported}");
Console.WriteLine($"  SSE4.1.X64:    {Sse41.X64.IsSupported}");
Console.WriteLine($"  SSE4.2.X64:    {Sse42.X64.IsSupported}");
Console.WriteLine($"  SSSE3.X64:     {Ssse3.X64.IsSupported}");
Console.WriteLine($"  X86Base.X64:   {X86Base.X64.IsSupported}");

Console.WriteLine("Supported Arm ISAs:");
Console.WriteLine($"  AdvSimd:       {AdvSimd.IsSupported}");
Console.WriteLine($"  Aes:           {ArmAes.IsSupported}");
Console.WriteLine($"  ArmBase:       {ArmBase.IsSupported}");
Console.WriteLine($"  Crc32:         {Crc32.IsSupported}");
Console.WriteLine($"  Dp:            {Dp.IsSupported}");
Console.WriteLine($"  Rdm:           {Rdm.IsSupported}");
Console.WriteLine($"  Sha1:          {Sha1.IsSupported}");
Console.WriteLine($"  Sha256:        {Sha256.IsSupported}");

Console.WriteLine("Supported Arm64 ISAs:");
Console.WriteLine($"  AdvSimd.Arm64: {AdvSimd.Arm64.IsSupported}");
Console.WriteLine($"  Aes.Arm64:     {ArmAes.Arm64.IsSupported}");
Console.WriteLine($"  ArmBase.Arm64: {ArmBase.Arm64.IsSupported}");
Console.WriteLine($"  Crc32.Arm64:   {Crc32.Arm64.IsSupported}");
Console.WriteLine($"  Dp.Arm64:      {Dp.Arm64.IsSupported}");
Console.WriteLine($"  Rdm.Arm64:     {Rdm.Arm64.IsSupported}");
Console.WriteLine($"  Sha1.Arm64:    {Sha1.Arm64.IsSupported}");
Console.WriteLine($"  Sha256.Arm64:  {Sha256.Arm64.IsSupported}");

Console.WriteLine("Supported Cross Platform ISAs:");
Console.WriteLine($"  Vector<T>:     {Vector.IsHardwareAccelerated}; {Vector<byte>.Count}");
Console.WriteLine($"  Vector64<T>:   {Vector64.IsHardwareAccelerated}");
Console.WriteLine($"  Vector128<T>:  {Vector128.IsHardwareAccelerated}");
Console.WriteLine($"  Vector256<T>:  {Vector256.IsHardwareAccelerated}");