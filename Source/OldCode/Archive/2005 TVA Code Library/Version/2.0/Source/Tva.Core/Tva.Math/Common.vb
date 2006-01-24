'*******************************************************************************************************
'  Tva.Math.Common.vb - Math Functions
'  Copyright � 2005 - TVA, all rights reserved - Gbtc
'
'  Build Environment: VB.NET, Visual Studio 2005
'  Primary Developer: James R Carroll, Operations Data Architecture [TVA]
'      Office: COO - TRNS/PWR ELEC SYS O, CHATTANOOGA, TN - MR 2W-C
'       Phone: 423/751-2827
'       Email: jrcarrol@tva.gov
'
'  Code Modification History:
'  -----------------------------------------------------------------------------------------------------
'  11/12/2004 - James R Carroll
'       Original version of source code generated
'  12/29/2005 - Pinal C Patel
'       2.0 version of source code migrated from 1.1 source (TVA.Shared.Math)
'  01/04/2006 - James R Carroll
'       Added crytographically strong random number generation functions
'
'*******************************************************************************************************

Imports Tva.Interop

Namespace Math

    ''' <summary>
    ''' Defines common math functions
    ''' </summary>
    Public NotInheritable Class Common

        Private Shared m_randomNumberGenerator As New Security.Cryptography.RNGCryptoServiceProvider

        Private Sub New()

            ' This class contains only global functions and is not meant to be instantiated

        End Sub

        ''' <summary>
        ''' <para>Calculates word length XOR check-sum on specified portion of a buffer.</para>
        ''' </summary>
        ''' <param name="data">Data buffer to perform XOR check-sum on</param>
        ''' <param name="startIndex">Start index in data buffer to begin XOR check-sum</param>
        ''' <param name="length">Total number of bytes from <paramref name="startIndex">startIndex</paramref> to perform XOR check-sum over</param>
        ''' <returns>Word length XOR check-sum</returns>
        Public Shared Function XorCheckSum(ByVal data As Byte(), ByVal startIndex As Integer, ByVal length As Integer) As Int16

            Dim sum As Int16

            For x As Integer = 0 To length - 1 Step 2
                sum = sum Xor BitConverter.ToInt16(data, startIndex + x)
            Next

            Return sum

        End Function

        ''' <summary>
        ''' <para>Generates a cryptographically strong floating-point random number between zero and one</para>
        ''' </summary>
        Public Shared ReadOnly Property RandomNumber() As Double
            Get
                Return BitwiseCast.ToUInt64(RandomInt64) / UInt64.MaxValue
            End Get
        End Property

        ''' <summary>
        ''' <para>Generates a cryptographically strong random boolean (i.e., coin toss)</para>
        ''' </summary>
        Public Shared ReadOnly Property RandomBoolean() As Boolean
            Get
                Dim value As Byte() = Array.CreateInstance(GetType(Byte), 1)

                m_randomNumberGenerator.GetBytes(value)

                Return IIf(value(0) Mod 2 = 0, True, False)
            End Get
        End Property

        ''' <summary>
        ''' <para>Generates a cryptographically strong 8-bit random integer</para>
        ''' </summary>
        Public Shared ReadOnly Property RandomByte() As Byte
            Get
                Dim value As Byte() = Array.CreateInstance(GetType(Byte), 1)

                m_randomNumberGenerator.GetBytes(value)

                Return value(0)
            End Get
        End Property

        ''' <summary>
        ''' <para>Generates a cryptographically strong 8-bit random integer between specified values</para>
        ''' </summary>
        Public Shared ReadOnly Property RandomByteBetween(ByVal startNumber As Byte, ByVal stopNumber As Byte) As Byte
            Get
                If stopNumber < startNumber Then Throw New ArgumentException("stopNumber must be greater than startNumber")
                Return Convert.ToByte(RandomNumber * (stopNumber - startNumber) + startNumber)
            End Get
        End Property

        ''' <summary>
        ''' <para>Generates a cryptographically strong 16-bit random integer</para>
        ''' </summary>
        Public Shared ReadOnly Property RandomInt16() As Int16
            Get
                Dim value As Byte() = Array.CreateInstance(GetType(Byte), 2)

                m_randomNumberGenerator.GetBytes(value)

                Return BitConverter.ToInt16(value, 0)
            End Get
        End Property

        ''' <summary>
        ''' <para>Generates a cryptographically strong 16-bit random integer between specified values</para>
        ''' </summary>
        Public Shared ReadOnly Property RandomInt16Between(ByVal startNumber As Int16, ByVal stopNumber As Int16) As Int16
            Get
                If stopNumber < startNumber Then Throw New ArgumentException("stopNumber must be greater than startNumber")
                Return Convert.ToInt16(RandomNumber * (stopNumber - startNumber) + startNumber)
            End Get
        End Property

        ''' <summary>
        ''' <para>Generates a cryptographically strong 32-bit random integer</para>
        ''' </summary>
        Public Shared ReadOnly Property RandomInt32() As Int32
            Get
                Dim value As Byte() = Array.CreateInstance(GetType(Byte), 4)

                m_randomNumberGenerator.GetBytes(value)

                Return BitConverter.ToInt32(value, 0)
            End Get
        End Property

        ''' <summary>
        ''' <para>Generates a cryptographically strong 32-bit random integer between specified values</para>
        ''' </summary>
        Public Shared ReadOnly Property RandomInt32Between(ByVal startNumber As Int32, ByVal stopNumber As Int32) As Int32
            Get
                If stopNumber < startNumber Then Throw New ArgumentException("stopNumber must be greater than startNumber")
                Return Convert.ToInt32(RandomNumber * (stopNumber - startNumber) + startNumber)
            End Get
        End Property

        ''' <summary>
        ''' <para>Generates a cryptographically strong 64-bit random integer</para>
        ''' </summary>
        Public Shared ReadOnly Property RandomInt64() As Int64
            Get
                Dim value As Byte() = Array.CreateInstance(GetType(Byte), 8)

                m_randomNumberGenerator.GetBytes(value)

                Return BitConverter.ToInt64(value, 0)
            End Get
        End Property

        ''' <summary>
        ''' <para>Generates a cryptographically strong 64-bit random integer between specified values</para>
        ''' </summary>
        Public Shared ReadOnly Property RandomInt64Between(ByVal startNumber As Int64, ByVal stopNumber As Int64) As Int64
            Get
                If stopNumber < startNumber Then Throw New ArgumentException("stopNumber must be greater than startNumber")
                Return Convert.ToInt64(RandomNumber * (stopNumber - startNumber) + startNumber)
            End Get
        End Property

        ''' <summary>
        ''' <para>Linear regression algorithm</para>
        ''' </summary>
        Public Shared Function CurveFit(ByVal polynomialOrder As Integer, ByVal pointCount As Integer, ByVal xValues As Double(), ByVal yValues As Double()) As Double()

            Dim coeffs(7) As Double
            Dim sum(21) As Double
            Dim v(11) As Double
            Dim b(11, 12) As Double
            Dim p As Double
            Dim divB As Double
            Dim fMultB As Double
            Dim sigma As Double
            Dim ls As Integer
            Dim lb As Integer
            Dim lv As Integer
            Dim i1 As Integer
            Dim i As Integer
            Dim j As Integer
            Dim k As Integer
            Dim l As Integer

            If Not (pointCount >= polynomialOrder + 1) Then Throw New ArgumentException("Point count must be greater than requested polynomial order")
            If Not (polynomialOrder >= 1) And (polynomialOrder <= 7) Then Throw New ArgumentOutOfRangeException("polynomialOrder", "Polynomial order must be between 1 and 7")

            ls = polynomialOrder * 2
            lb = polynomialOrder + 1
            lv = polynomialOrder
            sum(0) = pointCount

            For i = 0 To pointCount - 1
                p = 1.0#
                v(0) = v(0) + yValues(i)
                For j = 1 To lv
                    p = xValues(i) * p
                    sum(j) = sum(j) + p
                    v(j) = v(j) + yValues(i) * p
                Next
                For j = lb To ls
                    p = xValues(i) * p
                    sum(j) = sum(j) + p
                Next
            Next

            For i = 0 To lv
                For k = 0 To lv
                    b(k, i) = sum(k + i)
                Next
            Next

            For k = 0 To lv
                b(k, lb) = v(k)
            Next

            For l = 0 To lv
                divB = b(0, 0)
                For j = l To lb
                    If divB = 0 Then divB = 1
                    b(l, j) = b(l, j) / divB
                Next

                i1 = l + 1

                If i1 - lb < 0 Then
                    For i = i1 To lv
                        fMultB = b(i, l)
                        For j = l To lb
                            b(i, j) = b(i, j) - b(l, j) * fMultB
                        Next
                    Next
                Else
                    Exit For
                End If
            Next

            coeffs(lv) = b(lv, lb)
            i = lv

            Do
                sigma = 0
                For j = i To lv
                    sigma = sigma + b(i - 1, j) * coeffs(j)
                Next j
                i = i - 1
                coeffs(i) = b(i, lb) - sigma
            Loop While i - 1 > 0

            '    For i = 1 To 7
            '        Debug.Print "Coeffs(" & i & ") = " & Coeffs(i)
            '    Next i

            'For i = 1 To 60
            '    '        CalcY(i).TTag = xValues(1) + ((i - 1) / (xValues(pointCount) - xValues(1)))

            '    CalcY(i).TTag = ((i - 1) / 59) * xValues(pointCount) - xValues(1)
            '    CalcY(i).Value = Coeffs(1)

            '    For j = 1 To polynomialOrder
            '        CalcY(i).Value = CalcY(i).Value + Coeffs(j + 1) * CalcY(i).TTag ^ j
            '    Next
            'Next

            '    SSERROR = 0
            '    For i = 1 To pointCount
            '        SSERROR = SSERROR + (yValues(i) - CalcY(i).Value) * (yValues(i) - CalcY(i).Value)
            '    Next i
            '    SSERROR = SSERROR / (pointCount - polynomialOrder)
            '    sError = SSERROR

            ' Return slopes...
            Return coeffs

        End Function

    End Class

End Namespace
