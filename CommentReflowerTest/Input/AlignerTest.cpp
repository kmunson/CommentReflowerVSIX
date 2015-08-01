void main()
{
    int val = noParameters();

    int val2 = 
        noParametersAndComment( );  // comment

    oneParameter(theParameter);

    multiParameterOneLine( param1, param2() );

    int ian = multiParameterOneLineNoSpace(param1,param2);

    alreadyAlignedParams(param1,
                         "param2",
                         'p3',
                         param4);

    unalignedParams(param1,
                         "param2",
                         'p3',
              param4());

    secondFunctionOverMulti(hello, second(param1,
                                          params2));
                           
    unevenParams(param1, "param2",
        param3);

    secondOverMultLinesGetsShifted(params1,
        function1(param1,
                  params2),
        param3);

    unevenParamsWithSecond(param1, "\"param2", second(@"\param1",
                                          params2),
        param3);

    unalignedParamsWithComment(param1,
                         "param2",
              // comment
                      'p3',
                    param4);

    unalignedParamsWithMultlineCppComment(param1,
                         "param2",      // comment line 1
                                        // comment line 2
                             'p3',
                         param4);


    unalignedParamsWithBlockComment(param1,
                         "param2",
                     /* Mult line
                      * block comment
                      */
                         'p3',
                         param4);

    checkRet(encoder->encodeNextBlock(data +
                                            dataLineStride * y +
                                            dataPixelStride * x,
                                            3,
                                            dataLineStride));


}
