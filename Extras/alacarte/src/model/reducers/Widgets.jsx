//-------------------------------------------------------------------------------

import { combineReducers } from 'redux'
import _ from 'lodash'


const initial_state = {
}

//-------------------------------------------------------------------------------

function reduceById( state=initial_state, action ) {

  switch( action.type ) {
    
    case 'WIDGET_CREATE': {

      const { payload } = action
      const { id }      = payload
      const widget      = { ...payload }

      return { ...state, [id] : widget }
    }

    case 'WIDGET_REMOVE': {

      const { payload } = action
      const { id }      = payload

      return _.omit( state, id )
    }
  }

  return state
}


//-------------------------------------------------------------------------------

export default combineReducers({
  byId    : reduceById
})