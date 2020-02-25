# coding=utf-8
# --------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License. See License.txt in the project root for
# license information.
#
# Code generated by Microsoft (R) AutoRest Code Generator.
# Changes may cause incorrect behavior and will be lost if the code is
# regenerated.
# --------------------------------------------------------------------------

from msrest.serialization import Model


class ResultError(Model):
    """ResultError.

    :param code: Possible values include: 'Unknown', 'Failed', 'Conflict',
     'NotFound', 'ServerError', 'ValidationError', 'Unauthorized', 'Forbidden'
    :type code: str or ~teamcloud.models.enum
    :param message:
    :type message: str
    :param errors:
    :type errors: list[~teamcloud.models.ValidationError]
    """

    _attribute_map = {
        'code': {'key': 'code', 'type': 'str'},
        'message': {'key': 'message', 'type': 'str'},
        'errors': {'key': 'errors', 'type': '[ValidationError]'},
    }

    def __init__(self, *, code=None, message: str=None, errors=None, **kwargs) -> None:
        super(ResultError, self).__init__(**kwargs)
        self.code = code
        self.message = message
        self.errors = errors
