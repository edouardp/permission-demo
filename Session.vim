let SessionLoad = 1
if &cp | set nocp | endif
let s:so_save = &g:so | let s:siso_save = &g:siso | setg so=0 siso=0 | setl so=-1 siso=-1
let v:this_session=expand("<sfile>:p")
silent only
silent tabonly
cd ~/source/permissions2
if expand('%') == '' && !&modified && line('$') <= 1 && getline(1) == ''
  let s:wipebuf = bufnr('%')
endif
let s:shortmess_save = &shortmess
if &shortmess =~ 'A'
  set shortmess=aoOA
else
  set shortmess=aoO
endif
badd +1 src/PermissionsApi/Program.cs
badd +1 src/PermissionsApi/Controllers/PermissionsController.cs
badd +1 test/PermissionsAPI.ReqNRoll/PermissionsApi.feature
badd +1 test/PermissionsAPI.ReqNRoll/PermissionsApiSteps.cs
badd +18 src/PermissionsApi/Services/DataStore.cs
badd +1 test/PermissionsAPI.ReqNRoll/PermissionsApi.feature.cs
badd +5 src/PermissionsApi/Services/PermissionsRepository.cs
badd +22 src/PermissionsApi/Services/IPermissionsRepository.cs
badd +1 src/PermissionsApi/Models/Models.cs
badd +0 ~/notes
badd +82 test/PermissionsAPI.ReqNRoll/Groups.feature
badd +82 test/PermissionsAPI.ReqNRoll/DefaultPermissions.feature
badd +239 test/PermissionsAPI.ReqNRoll/Users.feature
badd +0 test/PermissionsAPI.ReqNRoll/DEBUG.feature
argglobal
%argdel
edit test/PermissionsAPI.ReqNRoll/DEBUG.feature
let s:save_splitbelow = &splitbelow
let s:save_splitright = &splitright
set splitbelow splitright
let &splitbelow = s:save_splitbelow
let &splitright = s:save_splitright
wincmd t
let s:save_winminheight = &winminheight
let s:save_winminwidth = &winminwidth
set winminheight=0
set winheight=1
set winminwidth=0
set winwidth=1
argglobal
balt test/PermissionsAPI.ReqNRoll/Users.feature
setlocal fdm=manual
setlocal fde=0
setlocal fmr={{{,}}}
setlocal fdi=#
setlocal fdl=0
setlocal fml=1
setlocal fdn=20
setlocal fen
silent! normal! zE
let &fdl = &fdl
let s:l = 1 - ((0 * winheight(0) + 41) / 82)
if s:l < 1 | let s:l = 1 | endif
keepjumps exe s:l
normal! zt
keepjumps 1
normal! 0
tabnext 1
if exists('s:wipebuf') && len(win_findbuf(s:wipebuf)) == 0
  silent exe 'bwipe ' . s:wipebuf
endif
unlet! s:wipebuf
set winheight=1 winwidth=20
let &shortmess = s:shortmess_save
let &winminheight = s:save_winminheight
let &winminwidth = s:save_winminwidth
let s:sx = expand("<sfile>:p:r")."x.vim"
if filereadable(s:sx)
  exe "source " . fnameescape(s:sx)
endif
let &g:so = s:so_save | let &g:siso = s:siso_save
let g:this_session = v:this_session
let g:this_obsession = v:this_session
doautoall SessionLoadPost
unlet SessionLoad
" vim: set ft=vim :
