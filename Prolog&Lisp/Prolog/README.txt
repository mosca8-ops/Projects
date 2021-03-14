Componenti gruppo: Andrea Moscatelli 833139, Matteo Pastori 829863.

Descrizione: 

- Con one_instruction/2 prende in ingresso uno stato e lo divide ricavando ogni
  suo elemento, cosi' da chiamare op/8 per effettuare l'operazione. Restituisce
  il nuovo stato.

- Con op/8 controlla di che istruzione si tratti e per ogni istruzione esegue 
  differenti calcoli. Qui viene chiamato frequentemente control_Pc/2 per
  controllare che pc sia tra 0 e 99 e che quando arriva a 99 riparta da 0.

- Nel codice seguono i predicati riguardanti le istruzioni di default con i
  vari passaggi per ottenere il risultato finale.
  
- Con execution_loop/2 per prima cosa controlla lo stato, se e' halted_state
  restituisce subito la lista di output. Altrimenti chiama ricorsivamente 
  one_instruction/2.
  
- lmc_run/3 trasforma il file assembly in codice con con lmc_load/2, esegue i
  controlli, e chiama execution_loop/2 con il risultato di lmc_load e l' input
  che si da in ingresso.
  
- lmc_load/2 riceve in ingresso il file assembly e restituisce la memoria in
  codici. Con uno stream legge il file e crea una stringa con il contenuto del
  file. Divide la stringa quando trova un carattere "\n". Toglie i commenti con
  togli_commento/4, rimuove gli spazi con delete_a/3 e crea una lista di liste
  con divide/3. Infine cerca le etichette e gli associa un valore con
  find_label/8, crea la memoria finale con crea_mem/6 e controlla la memoria
  appena creata prima di restituirla.
  
- crea_mem/6 ricorsivamente chiama val_op/4 che gli restituisce i valori della
  memoria finale. crea_mem/6 li concatena e li restituisce se ha fatto scorrere
  tutto l' input.
  
- val_op/4 semplicemente confronta la stringa in input (che e' sicuramente un
  istruzione) con le varie possibilita e gli associa un valore, controllando
  anche se l' istruzione ha un etichetta o meno.