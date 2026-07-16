# Around Design Patterns

| Mediator  | Observer |
| :-----------: |:-------------:|
| left foo      | right foo     |
| left bar      | right bar     |
| left baz      | right baz     |

<table>
  <tr>
    <th align="center">Mediator</th>
    <th align="center">Observer</th>
  </tr>
  <tr>
    <td align="center">
        <a href="https://github.com/mamagarage/mama-design-patterns/tree/main/src/Mediator"> 
          <img alt="" width="200" src="https://github.com/mamagarage/mama-design-patterns/blob/main/img/mediator.jpeg?raw=true" alt=""/>
        </a>
    </td>
    <td align="center">
        <!--
        <img alt="" width="400" src="https://github.com/mamagarage/mama-design-patterns/blob/main/img/mediator.jpeg" alt=""></img>
        -->
    </td>
  </tr>
</table>

## Mermaid diagrams
```mermaid
graph TD;
    A-->B;
    A-->C;
    B-->D;
    C-->D;
```
```mermaid
flowchart TD
    docx[Word File]
    docx --> forge
    forge[["`**xBRL-Forge**
    convert to JSON Structure`"]]
    forge --> json1
    json1[("**Document Contents in 
    Target JSON strucutre**
    edit (tag) with any Tool and feed back into xBRL-Forge to create xBRL Package")]
```
