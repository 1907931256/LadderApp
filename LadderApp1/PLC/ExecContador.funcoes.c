//====================================================================================
// Rotina de execu��o do contador
//====================================================================================
void ExecContador(TContador *Contador)
{
	switch (Contador->Tipo)
	{
#EXECCONTADOR_TIPO0#
#EXECCONTADOR_TIPO1#
	default:
		break;
	}
	if (Contador->EN == 0)
		Contador->Pulso = 1;
}
