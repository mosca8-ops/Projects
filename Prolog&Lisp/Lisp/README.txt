Componenti gruppo: Pastori Matteo 829863, Moscatelli Andrea 833139

Descrizione:

- Il file contentente le istruzioni assembly che si vogliono eseguire
  viene letto dalla funzione "recursive" e il contenuto di tale file viene
  inserito all'interno di una unica stringa tramite la funzione "read-file"
  e poi la stringa ritornata viene divisa in una lista di stringhe, dove 
  ogni elemento di tale stringa corrisponde a una riga del file assembly

- In "lmc-load" vengono create delle variabili locali: valori-etichette che 
  contiene i valori delle etichette presenti nel file assembly, 
  l-etichette che contiene tutte le etichette del file assembly,
  lista3 che è una lista intermedia per arrivare poi alla lista della 
  memoria iniziale. Lista3 viene creata prendendo la lista di stringhe 
  creata da "get-file" e poi passata in successione prima a "togli-commenti"
  che elimina tutti i commenti presenti nel file assembly, poi a "doppia-l"
  che crea una lista di liste dove ogni elemento della lista passatagli 
  diventa a sua volta una lista di stringhe, poi a "elimina-stringhe-vuote"
  che che elimina eventuali stringhe vuote dovute a commenti di un'intera 
  riga presenti nel file assembly, infine viene passata a "cancella-eti" 
  che cancella le etichette che si trovano in prima posizione di ciascuna 
  lista della lista principale. Infine tramite prima la funzione "val-mem" 
  che assegna un valore numerico alla lista precedente, poi tramite 
  "zero-aggiungi" che se la lista ha meno di 100 elementi aggiunge degli 
  zero in coda alla lista per arrivare a 100 elementi, "lmc-load" 
  restituisce la memoria iniziale dell'esecutore.

- Per la parte dell'esecutore tramite la funzione "execution-loop" viene 
  richiamata la funzione "one-instruction" che permette di modificare lo 
  stato iniziale rispetto all'istruzione che deve eseguire. Se lo stato
  diventa "helted-state" l'esecuzione termina e ritorna la lista di output.
  
- L'esecuzione del programma viene effettuata tramite "lmc-run", che chiama
  "lmc-load" creando la memoria iniziale e poi passa tale memoria a 
  "execution-loop" oltre agli altri elementi necessari per lo stato iniziale.