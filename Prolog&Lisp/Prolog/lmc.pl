%%%% -*- Mode: Prolog -*-
%%%% lmc.pl


%%%% Pastori Matteo 829863
%%%% Moscatelli Andrea 833139


%%% mod1/3
%%% Modulo di M così da ottenere le ultime cifre di un numero.

mod1(X, M, Y) :-
    Y is X mod M.


%%% control_Pc/3
%%% Controllo che Pc è compreso tra 0 e 99
%%% e che quando arriva a 99 ricomincia da 0.

control_Pc(Pc, New_Pc) :-
    Pc < 99, !,
    New_Pc is Pc + 1.


control_Pc(Pc, New_Pc) :-
    Pc = 99,
    New_Pc is 0.


%%% one_instruction/2
%%% Crea un nuovo stato dopo aver eseguito l'istruzione.

one_instruction(State, NewState) :-
    State =.. X,
    nth0(0, X, Pred), Pred = state,
    nth0(1, X, Acc),
    nth0(2, X, Pc),
    nth0(3, X, Mem),
    nth0(4, X, In),
    nth0(5, X, Out),
    nth0(6, X, Flag),
    nth0(Pc, Mem, Result),
    op(Result, Acc, Mem, In, Out, Pc, Flag, L),
    NewState =.. L.


%%% op/8
%%% Con op controllo il valore della cella di memoria per
%%% capire quale operazione fare e creo un nuovo stato con
%%% i valori calcolati in base all' operazione.

%%% Halt.

op(CellMem, Acc, Mem, In, Out, Pc, Flag, L) :-
    between(0, 99, CellMem), !,
    L = [halted_state, Acc, Pc, Mem, In, Out, Flag].


%%% Addizione.

op(CellMem, Acc, Mem, In, Out, Pc, _Flag, L) :-
    between(100, 199, CellMem), !,
    add(Mem, CellMem, Acc, Result, F),
    control_Pc(Pc, New_Pc),
    L = [state, Result, New_Pc, Mem, In, Out, F],
    !.


%%% Sottrazione.

op(CellMem, Acc, Mem, In, Out, Pc, _Flag, L) :-
    between(200, 299, CellMem), !,
    sub(Mem, CellMem, Acc, Result, F),
    control_Pc(Pc, New_Pc),
    L = [state, Result, New_Pc, Mem, In, Out, F],
    !.


%%% Store.

op(CellMem, Acc, Mem, In, Out, Pc, Flag, L) :-
    between(300, 399, CellMem), !,
    store(CellMem, Acc, Mem, List),
    control_Pc(Pc, New_Pc),
    L = [state, Acc, New_Pc, List, In, Out, Flag].


%%% Errore.

op(CellMem, _Acc,_Mem, _In, _Out,_Pc, _Flag, _L) :-
    between(400, 499, CellMem), !,
    fail.


%%% Load.

op(CellMem, _Acc, Mem, In, Out, Pc, Flag, L) :-
    between(500, 599, CellMem), !,
    load(CellMem, Mem, R),
    control_Pc(Pc, New_Pc),
    L = [state, R, New_Pc, Mem, In, Out, Flag].


%%% Branch.

op(CellMem, Acc, Mem, In, Out, _Pc, Flag, L) :-
    between(600, 699, CellMem), !,
    mod1(CellMem, 100, Y),
    New_Pc is Y,
    L = [state, Acc, New_Pc, Mem, In, Out, Flag].


%%% Branch if zero.

op(CellMem, Acc, Mem, In, Out, Pc, Flag, R) :-
    between(700, 799, CellMem),
    mod1(CellMem, 100, Y),
    branchZ(Y, Acc, Mem, Pc, In, Out, Flag, R), !.


%%% Branch if positive.

op(CellMem, Acc, Mem, In, Out, Pc, Flag, R) :-
    between(800, 899, CellMem),
    mod1(CellMem, 100, Y),
    branchP(Y, Acc, Mem, Pc, In, Out, Flag, R), !.


%%% Input.

op(CellMem, _Acc, Mem, [In | Coda], Out, Pc, Flag, L) :-
    CellMem = 901, !,
    empty_list(In, Coda),
    New_Acc is In,
    control_Pc(Pc, New_Pc),
    L = [state, New_Acc, New_Pc, Mem, Coda, Out, Flag].


%%% Output.

op(CellMem, Acc, Mem, In, Out, Pc, Flag, L) :-
    CellMem = 902, !,
    append(Out, [Acc], New_Out),
    control_Pc(Pc, New_Pc),
    L = [state, Acc, New_Pc, Mem, In, New_Out, Flag].


%%% add/5
%%% Istruzione di addizione, eseguo i calcoli.

add(Mem, CellMem, Acc, R, Flag) :-
    mod1(CellMem, 100, Y),
    nth0(Y, Mem, K),
    R is K + Acc,
    R < 1000, Flag = noflag, !.


add(Mem, CellMem, Acc, R, Flag) :-
    mod1(CellMem, 100, Y),
    nth0(Y, Mem, K),
    R is (K + Acc) mod 1000,
    Flag = flag.


%%% sub/5
%%% Istruzione di sottrazione, eseguo i calcoli.

sub(Mem, CellMem, Acc, R, Flag) :-
    mod1(CellMem, 100, Y),
    nth0(Y, Mem, K),
    X is (Acc - K),
    X >= 0, Flag = noflag, !,
    R is X mod 1000.


sub(Mem, CellMem, Acc, R, Flag) :-
    mod1(CellMem, 100, Y),
    nth0(Y, Mem, K),
    X is (Acc - K),
    X < 0, Flag = flag, !,
    R is X mod 1000.


%%% store/4
%%% Istruzione di store, eseguo i calcoli.

store(CellMem, Acc, Mem, L) :-
    mod1(CellMem, 100, Y),
    insert1(Mem, Acc, Y, [], L).


%%% insert1/5
%%% Inserisce N (Accumulatore) nella memoria.

insert1([_List | Lists], N, Cont, New_list, Lista) :-
    Cont = 0, !,
    append([N], Lists, F),
    append(New_list, F, Lista).


insert1([List | Lists], N, Cont, New_list, L) :-
    Cont \= 0, !,
    append(New_list, [List], A),
    R is Cont - 1,
    insert1(Lists, N, R, A, L).


%%% load/3
%%% Istruzione di load, eseguo i calcoli.

load(CellMem, Mem, R) :-
    mod1(CellMem, 100, Y),
    nth0(Y, Mem, K),
    R is K.


%%% branchZ/8
%%% Istruzione di branch if zero, eseguo i calcoli.

branchZ(_Y, Acc, Mem, Pc, In, Out, Flag, R) :-
    Acc \= 0, !,
    control_Pc(Pc, New_Pc),
    R = [state, Acc, New_Pc, Mem, In, Out, Flag].


branchZ(_Y, Acc, Mem, Pc, In, Out, Flag, R) :-
    Flag = flag, !,
    control_Pc(Pc, New_Pc),
    R = [state, Acc, New_Pc, Mem, In, Out, Flag].


branchZ(Y, Acc, Mem, _Pc, In, Out, Flag, R) :-
    Acc = 0, Flag = noflag,
    R = [state, Acc, Y, Mem, In, Out, Flag].


%%% branchP/8
%%% Istruzione di branch if positive, eseguo i calcoli.

branchP(_Y, Acc, Mem, Pc, In, Out, Flag, R) :-
    Flag = flag, !,
    control_Pc(Pc, New_Pc),
    R = [state, Acc, New_Pc, Mem, In, Out, Flag].


branchP(Y, Acc, Mem, _Pc, In, Out, Flag, R) :-
    Flag = noflag,
    R = [state, Acc, Y, Mem, In, Out, Flag].


%%% empty_list/2
%%% Controlla che la lista In sia vuota, se si fallisce.

empty_list(In, Coda) :-
    In = [],
    Coda = [], !,
    fail.


empty_list(In, _Coda) :-
    In \= [].


%%% execution_loop/2
%%% Esegue one_instruction e se lo stato e' "state" richiama
%%% ricorsivamente, altrimenti restituisce la lista di output.

execution_loop(State, Out) :-
    %% Verifica il nome dello Stato
    State =.. K,
    nth0(0, K, Name_state),
    Name_state = state,
    %% Se lo Stato è state procede con la ricorsione.
    one_instruction(State, New_State),
    execution_loop(New_State, Out), !.


execution_loop(State, Out) :-
    %% Verifica il nome dello stato.
    State =.. K,
    nth0(0, K, Name_state),
    Name_state = halted_state,
    %% Se lo Stato è halted_state restituisce la lista di output.
    nth0(5, K, Out), !.


%%% lmc_run/3
%%% Carica la memoria con lmc_load e chiama execution_loop per
%%% per eseguire.

lmc_run(Filename, Input, Output) :-
    lmc_load(Filename, R),
    %% Controlla memoria tra 0 e 999.
    loop(R),
    %% Controllo input tra 0 e 999.
    loop(Input),
    execution_loop(state(0, 0, R, Input, [], noflag), Output),
    %% Controllo output tra 0 e 999.
    loop(Output).

%%% lmc_load/2
%%% Legge un file assembly e crea una nuova memoria.

lmc_load(Filename, Mem) :-
    open(Filename, read, In),
    read_string(In, _, X),
    close(In),
    %% Divisione della stringa in lista.
    split_string(X, "\n", "", R),
    togli_commento(R, [], 0, K),
    delete_a(K, Lista_finale, []),
    divide(Lista_finale, Memoria, []),
    %% Q = lista delle etichette, T = valore corrispondente.
    find_label(Memoria, Nuova_Memoria, [], 0, [], [], Labels, Val_labels),
    crea_mem(Nuova_Memoria, B, Labels, Val_labels, 0, []),
    control_mem(B, Mem).


%%% crea_mem/6
%%% Crea nuova memoria richiamando val_op/4.

crea_mem(Nuova_Memoria, Final_mem, _L1, _L2, N, Mem_app) :-
    length(Nuova_Memoria, X),
    N = X, !,
    Final_mem = Mem_app.


crea_mem(Nuova_Memoria, Final_mem, L1, L2, N, Mem_app) :-
    length(Nuova_Memoria, X),
    N < X, !,
    nth0(N, Nuova_Memoria, K),
    val_op(K, L1, L2, R),
    append(Mem_app, [R], F),
    New_N is N + 1,
    crea_mem(Nuova_Memoria, Final_mem, L1, L2, New_N, F).


%%% val_op/4
%%% Ritorna il valore dell' operazione corrispondente alla stringa.

%%% Input.

val_op(K, _L1, _L2, R) :-
    length(K, Z),
    Z = 1,
    nth0(0, K, G),
    G = "INP", !,
    R is 901.


%%% Output.

val_op(K, _L1, _L2, R) :-
    length(K, Z),
    Z = 1,
    nth0(0, K, G),
    G = "OUT", !,
    R is 902.


%%% Halt.

val_op(K, _L1, _L2, R) :-
    length(K, Z),
    Z = 1,
    nth0(0, K, G),
    G = "HLT", !,
    R is 0.


%%% Add.

val_op(K, L1, L2, R) :-
    length(K, Z),
    Z = 2,
    nth0(0, K, G),
    G = "ADD", !,
    nth0(1, K, F),
    val_et(F, X, L1, L2), !,
    R is 100 + X.


%%% Sub.

val_op(K, L1, L2, R) :-
    length(K, Z),
    Z = 2,
    nth0(0, K, G),
    G = "SUB", !,
    nth0(1, K, F),
    val_et(F, X, L1, L2), !,
    R is 200 + X.


%%% Store.

val_op(K, L1, L2, R) :-
    length(K, Z),
    Z = 2,
    nth0(0, K, G),
    G = "STA", !,
    nth0(1, K, F),
    val_et(F, X, L1, L2), !,
    R is 300 + X.


%%% Load.

val_op(K, L1, L2, R) :-
    length(K, Z),
    Z = 2,
    nth0(0, K, G),
    G = "LDA", !,
    nth0(1, K, F),
    val_et(F, X, L1, L2), !,
    R is 500 + X.


%%% Branch.

val_op(K, L1, L2, R) :-
    length(K, Z),
    Z = 2,
    nth0(0, K, G),
    G = "BRA", !,
    nth0(1, K, F),
    val_et(F, X, L1, L2), !,
    R is 600 + X.


%%% Branch if zero.

val_op(K, L1, L2, R) :-
    length(K, Z),
    Z = 2,
    nth0(0, K, G),
    G = "BRZ", !,
    nth0(1, K, F),
    val_et(F, X, L1, L2), !,
    R is 700 + X.


%%% Branch if positive.

val_op(K, L1, L2, R) :-
    length(K, Z),
    Z = 2,
    nth0(0, K, G),
    G = "BRP", !,
    nth0(1, K, F),
    val_et(F, X, L1, L2), !,
    R is 800 + X.


%%% Dat senza valore.

val_op(K, _L1, _L2, R) :-
    nth0(0, K, G),
    length(K, V),
    V = 1, !,
    G = "DAT", !,
    R is 0.


%%% Dat con valore.

val_op(K, L1, L2, R) :-
    length(K, Z),
    Z = 2,
    nth0(0, K, G),
    G = "DAT", !,
    nth0(1, K, F), !,
    val_et(F, X, L1, L2), !,
    R is X.


%%% val_et/4
%%% Controlla se la stringa e' un numero e lo ritorna.
%%% Altrimenti cerca il valore con search_value/5.

val_et(F, X, _L1, _L2) :-
    atom_string(Y, F),
    atom_number(Y, X), !.


val_et(F, X, L1, L2) :-
   search_value(L1, L2, F, X, 0).


%%% find_label/8
%%% Ritorna la memoria senza le etichette iniziali, una lista
%%% di etichette trovate e una lista con i valori corrispondenti
%%% alle etichette trovate.

find_label(Mem, New_R, Mem_App, N, Label, V_label, Q, T) :-
    length(Mem, K),
    N < K, !,
    nth0(N, Mem, R),
    label_a(R, New_M, N, Label, V_label, X, Y),
    append(Mem_App, [New_M], W),
    New_N is N + 1,
    find_label(Mem, New_R, W, New_N, X, Y, Q, T).


find_label(Mem, New_R, Mem_App, N, Label, V_label, Q, T) :-
    length(Mem, K),
    N = K, !,
    New_R = Mem_App,
    Q = Label,
    T = V_label.


%%% label_a/7
%%% Prende una lista rappresentante una riga del file assembly e
%%% restituisce: la riga, eventualmente senza etichetta iniziale;
%%% la lista con aggiunta, se presente, l'etichetta; la lista con
%%% il valore dell'etichetta.

label_a(R, New_M, N, Label, V_label, X, Y) :-
    nth0(0, R, K),
    % controllo se è istruzione o no
    method_control(K, Result),
    Result = "f", !,
    delete_label(R, New_M),
    append(Label, [K], X),
    append(V_label, [N], Y).


label_a(R, New_M, _N, Label, V_label, X, Y) :-
    nth0(0, R, K),
    % controllo se è istruzione o no
    method_control(K, Result),
    Result = "t", !,
    New_M = R,
    X = Label,
    Y = V_label.


%%% method_control/2
%%% Se la stringa (K) corrisponde ad un istruzione ritorna t,
%%% altrimenti f.

method_control(K, Result) :-
    K \= "ADD",
    K \= "SUB",
    K \= "STA",
    K \= "LDA",
    K \= "BRA",
    K \= "BRZ",
    K \= "BRP",
    K \= "INP",
    K \= "OUT",
    K \= "HLT",
    K \= "DAT",
    Result = "f", !.


method_control(_K, Result) :-
    Result = "t", !.


%%% delete_label/2
%%% Ritorna la lista in input senza il primo elemento.

delete_label([_X | R], New_R) :-
    New_R = R.


%%% delete_a/3
%%% Rimuove gli spazi della lista in input.

delete_a([X | T], List, A) :-
    X = ' ',
    delete_a(T, List, A), !.


delete_a([X | T], List, A) :-
    X \= ' ',
    append(A, [X], N),
    delete_a(T, List, N), !.


delete_a(X, List, A) :-
    X = [],
    List = A, !.


%%% divide/3
%%% Crea una lista di liste

divide(X, Mem, App) :-
    X = [], !,
    Mem = App.


divide([X | List], Mem, App) :-
    List = [],
    atom_string(X, R),
    split_string(R, " ", " ", T),
    append(App, [T], Mem), !.


divide([X | List], Mem, App) :-
    atom_string(X, R),
    split_string(R, " ", " ", T),
    append(App, [T], K),
    divide(List, Mem, K).


%%% search_value/5
%%% Cerca il valore corrispondente all'etichetta.

search_value(L1, L2, S, R, N) :-  % dalla stringa trovo il valore
    nth0(N, L1, K),
    K = S, !,
    nth0(N, L2, R).


search_value(L1, L2, S, R, N) :-
    nth0(N, L1, K),
    K \= S, !,
    New_N is N + 1,
    search_value(L1, L2, S, R, New_N).


%%% togli_commento/4
%%% Elimina i commenti.

togli_commento(R, New_R, N, Lista_finale) :-
    length(R, Lunghezza),
    N < Lunghezza,
    nth0(N, R, K),
    string_codes(K, J),
    commento(J, 0, [], Risultato),
    string_codes(G, Risultato),
    New_N is N +1,
    %% Elimina gli spazi.
    split_string(G, " \t", "\s\t\n", H),
    unisci(H, A, 0, ''),
    append(New_R, [A], Nuova_lista),
    togli_commento(R, Nuova_lista, New_N, Lista_finale), !.


togli_commento(R, New_R, N, Lista_finale) :-
    length(R, Lunghezza),
    N = Lunghezza,
    Lista_finale = New_R.


%%% unisci/4
%%% Concatena gli elemnti della lista H dopo aver trasformato
%%% i caratteri in maiuscolo, mettendo uno spazio dopo ogni elemento.

unisci(H, A, N, Lista_parziale) :-
   length(H, Lunghezza),
   N < Lunghezza,
   nth0(N, H, R),
   atom_concat(Lista_parziale, R, New_List),
   string_upper(New_List, S),
   atom_concat(S, ' ', L),
   New_N is N +1,
   unisci(H, A, New_N, L), !.


unisci(H, A, N, Lista_parziale) :-
    length(H, Lunghezza),
    N = Lunghezza,
    A = Lista_parziale.


%%% commento/4
%%% Elimina il resto dei codici dopo "//" compresi, altrimenti
%%% se ce n'e' solo uno ritorna un errore.

commento(Lista, N, New_List, Risultato) :-
    nth0(N, Lista, R),
    R \= 47,
    append(New_List, [R], K),
    New_N is N + 1,
    commento(Lista, New_N, K, Risultato), !.


commento(Lista, N, New_List, Risultato) :-
    nth0(N, Lista, R),
    R = 47,
    N_N is N +1,
    nth0(N_N, Lista, F),
    F = 47,
    Risultato = New_List, !.


commento(Lista, N, New_List, Risultato) :-
    length(Lista, Lunghezza),
    N = Lunghezza,
    Risultato = New_List.


%%% control_mem/2
%%% Ritorna la memoria con 100 elementi.

control_mem(List, Mem) :-   % memoria con 100 elementi
    length(List, Lung),
    Lung < 101,
    X is 100 - Lung,
    zero_add(List, X, Mem).


%%% zero_add/3
%%% Aggiunge tanti zeri tanto quanto il parametro X.

zero_add(List, X, Mem) :-
    X = 0,
    Mem = List, !.


zero_add(List, X, Mem) :-
    X > 0,
    Y is X - 1,
    append(List, [0], Par),
    zero_add(Par, Y, Mem).


%%% loop/1
%%% Controlla che i numeri della lista in input siano
%%% tra 0 e 999.

loop([]).


loop([X | Xs]) :- between(0, 999, X), loop(Xs).


%%%% end of file -- lmc.pl
